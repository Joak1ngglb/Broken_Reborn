using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Pets;
using Intersect.GameObjects;
using Intersect.Server.Entities;
using Intersect.Server.Maps;
using Intersect.Utilities;

namespace Intersect.Server.AI.Pets
{
    /// <summary>
    /// Preferencias de IA por defecto. Puedes exponer esto en editor más adelante.
    /// </summary>
    public sealed class PetAIConfig
    {
        // Rangos
        public int LeashTiles { get; set; } = 8;                 // distancia máxima desde el dueño
        public int ChaseRangeTiles { get; set; } = 10;           // persecución de enemigos
        public int DefendTriggerRangeTiles { get; set; } = 8;    // rango para reaccionar al agresor

        // Curación y soporte
        public int HealOwnerThresholdPercent { get; set; } = 50; // compatibilidad retro; usa histéresis
        public int HealSelfThresholdPercent { get; set; } = 40;  // si pet < 40% HP intenta curarse
        public int MinManaPercentToCast { get; set; } = 15;      // si mana < 15% evita castear
        public int ManaReserveForEmergencyPercent { get; set; } = 20; // reserva para curas
        public int HealOwnerThresholdEnterPercent { get; set; } = 50; // umbral activar
        public int HealOwnerThresholdExitPercent { get; set; } = 60;  // umbral salir (histéresis)

        // Cadencias
        public long ThinkIntervalMs { get; set; } = 200;         // período de evaluación
        public long RetargetIntervalMs { get; set; } = 600;      // cambio de objetivo
        public long RepathIntervalMs { get; set; } = 250;        // mover/seguir
        public long GlobalCooldownMs { get; set; } = 300;        // GCD general

        // Seguridad
        public bool NeverPullAggroAlone { get; set; } = true;    // si owner no está en combate, evita iniciar combates lejos
        public int PanicFleeBelowSelfPercent { get; set; } = 20;
        public int PanicFleeTiles { get; set; } = 3;
    }

    /// <summary>
    /// Blackboard: estado efímero que la IA consulta/actualiza.
    /// </summary>
    public sealed class PetAIBlackboard
    {
        public long LastThinkTime;
        public long LastRetargetTime;
        public long LastPathTime;

        public Entity? LastOwnerAttacker;
        public long LastOwnerDamagedAt;

        public Entity? CurrentTarget;
        public long CurrentTargetSince;

        public readonly Dictionary<Guid, long> BlacklistUntil = new(); // entityId -> ms
        public readonly Dictionary<Guid, int> Threat = new(); // entityId -> threat
    }

    public interface IPetBrain
    {
        void Update(long timeMs);
        void OnOwnerDamaged(Entity? attacker);
        void OnHit(Entity attacker, int damage);
        void OnBehaviorChanged(PetState newBehavior);
        void OnDied();
        void Reset();
    }

    /// <summary>
    /// Contrato mínimo que usamos del Pet (server) para no acoplar fuerte.
    /// </summary>
    public interface IPetRuntime
    {
        Guid Id { get; }
        bool IsDisposed { get; }
        bool IsDead { get; }
        int X { get; }
        int Y { get; }
        Direction Dir { get; }
        Guid MapId { get; }
        Guid MapInstanceId { get; }
        Player? Owner { get; }
        PetState Behavior { get; }

        long[] MaxVitals { get; }
        long[] Vitals { get; }

        bool HasManaFor(SpellDescriptor spell);
        bool IsInLineOfSight(Entity target);
        bool IsInRange(Entity target, int tilesRange);
        bool CanCast(SpellDescriptor spell, out string? reason);
        bool TryCastSpell(SpellDescriptor spell, Entity? targetEntity, int? tx = null, int? ty = null);

        bool TryMoveToward(int tx, int ty);
        bool TryStepAwayFrom(Entity from, int tiles = 1);
        bool TryFace(Entity target);

        // Accesos al descriptor de pet
        PetDescriptor? Descriptor { get; }
        IEnumerable<SpellDescriptor> GetUsableSpells(); // indexa las spells del pet que pueden castear (ofensivas/soporte)
    }

    /// <summary>
    /// Implementación por defecto de la IA de mascotas.
    /// </summary>
    public sealed class PetAIController : IPetBrain
    {
        private readonly IPetRuntime _pet;
        private readonly PetAIConfig _cfg;
        private readonly PetAIBlackboard _bb = new();

        // Cooldowns por spell
        private readonly Dictionary<Guid, long> _spellCdUntil = new();

        private long _gcdUntilMs;
        private bool _lastHealedOwner;
        private int _consecutivePathFails;
        private (int x, int y) _lastPos;

        public PetAIController(IPetRuntime pet, PetAIConfig? cfg = null)
        {
            _pet = pet ?? throw new ArgumentNullException(nameof(pet));
            _cfg = cfg ?? new PetAIConfig();
        }

        public Action<string>? OnDebug { get; set; }

        public void Reset()
        {
            _bb.LastThinkTime = 0;
            _bb.LastRetargetTime = 0;
            _bb.LastPathTime = 0;
            _bb.LastOwnerAttacker = null;
            _bb.LastOwnerDamagedAt = 0;
            _bb.CurrentTarget = null;
            _bb.CurrentTargetSince = 0;
            _spellCdUntil.Clear();
            _bb.BlacklistUntil.Clear();
            _bb.Threat.Clear();
            _gcdUntilMs = 0;
            _lastHealedOwner = false;
            _consecutivePathFails = 0;
            _lastPos = default;
        }

        public void OnDied()
        {
            Reset();
        }

        public void OnBehaviorChanged(PetState newBehavior)
        {
            // liberar objetivo si pasa a Passive/Stay
            if (newBehavior is PetState.Passive or PetState.Stay)
            {
                _bb.CurrentTarget = null;
            }
        }

        public void OnOwnerDamaged(Entity? attacker)
        {
            if (attacker == null || attacker.IsDisposed || attacker.IsDead)
            {
                return;
            }

            _bb.LastOwnerAttacker = attacker;
            _bb.LastOwnerDamagedAt = Timing.Global.Milliseconds;
            AddThreat(attacker, amount: 10);
            Log($"Owner damaged by {attacker.Id}, threat now {_bb.Threat[attacker.Id]}");
        }

        public void OnHit(Entity attacker, int damage)
        {
            if (attacker == null || attacker.IsDisposed || attacker.IsDead)
            {
                return;
            }

            AddThreat(attacker, Math.Max(1, damage / 5));
        }

        public void Update(long timeMs)
        {
            if (_pet.IsDisposed || _pet.IsDead)
            {
                return;
            }

            // Throttle de pensamiento
            if (timeMs < _bb.LastThinkTime + _cfg.ThinkIntervalMs)
            {
                return;
            }

            _bb.LastThinkTime = timeMs;

            var owner = _pet.Owner;
            if (owner == null || owner.IsDisposed)
            {
                return;
            }

            // 0) Mantener leash (no irse muy lejos del owner)
            if (!WithinTiles(_pet.X, _pet.Y, owner.X, owner.Y, _cfg.LeashTiles))
            {
                // Si se alejó, prioriza volver
                _ = MoveToward(owner.X, owner.Y);
                return;
            }

            // 1) Curar (dueño o pet) si hace falta
            if (TryHealIfNeeded(owner))
            {
                return;
            }

            if (PanicFleeIfNecessary(owner))
            {
                return;
            }

            // 2) Selección/actualización de objetivo según behavior
            UpdateTarget(timeMs, owner);

            // 3) Intentar ofensiva sobre target actual
            if (TryOffense())
            {
                return;
            }

            var currentTarget = _bb.CurrentTarget;
            var hasValidTarget = IsValidEnemy(currentTarget);
            if (!hasValidTarget)
            {
                _bb.CurrentTarget = null;
            }

            if (hasValidTarget && currentTarget != null)
            {
                // Mantener persecución o posicionamiento en torno al objetivo actual
                MoveIntoAttackRange(currentTarget);
                return;
            }

            // 4) Posicionamiento según behavior cuando no hay objetivo válido
            switch (_pet.Behavior)
            {
                case PetState.Follow:
                    MoveNearOwner(owner, preferredDistance: 1, maxDistance: 2);
                    break;

                case PetState.Defend:
                    // si no hay target, estructura similar a follow
                    MoveNearOwner(owner, preferredDistance: 1, maxDistance: 2);
                    break;

                case PetState.Stay:
                    // quieta; quizá orientar hacia el owner
                    _pet.TryFace(owner);
                    break;

                case PetState.Passive:
                    // idle suave: acercarse un poco si se aleja demasiado
                    if (!WithinTiles(_pet.X, _pet.Y, owner.X, owner.Y, 3))
                    {
                        MoveNearOwner(owner, preferredDistance: 2, maxDistance: 3);
                    }

                    break;
            }
        }

        // ---------- Decisiones de Curación ----------
        private bool TryHealIfNeeded(Player owner)
        {
            var manaPct = Percent(_pet.Vitals[(int)Vital.Mana], _pet.MaxVitals[(int)Vital.Mana]);
            if (manaPct < _cfg.MinManaPercentToCast)
            {
                return false;
            }

            var ownerHpPct = Percent(owner.GetVital(Vital.Health), owner.GetMaxVital(Vital.Health));
            var enterThreshold = _cfg.HealOwnerThresholdEnterPercent;
            if (enterThreshold <= 0)
            {
                enterThreshold = _cfg.HealOwnerThresholdPercent;
            }

            var exitThreshold = _cfg.HealOwnerThresholdExitPercent;
            if (exitThreshold <= 0)
            {
                exitThreshold = Math.Max(enterThreshold + 5, enterThreshold);
            }

            var needHealOwner = ownerHpPct < enterThreshold;
            var safeOwner = ownerHpPct > exitThreshold;

            if (needHealOwner || (_lastHealedOwner && !safeOwner))
            {
                var heal = FindBestHealSpell(targetSelf: false);
                if (heal != null && TryCastWithCd(heal, owner))
                {
                    _lastHealedOwner = true;
                    return true;
                }
            }

            _lastHealedOwner = false;

            var selfHpPct = Percent(_pet.Vitals[(int)Vital.Health], _pet.MaxVitals[(int)Vital.Health]);
            if (selfHpPct < _cfg.HealSelfThresholdPercent)
            {
                var selfHeal = FindBestHealSpell(targetSelf: true);
                if (selfHeal != null && TryCastWithCd(selfHeal, _pet as Entity))
                {
                    return true;
                }
            }

            return false;
        }

        private SpellDescriptor? FindBestHealSpell(bool targetSelf)
        {
            return _pet
                .GetUsableSpells()
                .Where(IsHealingSpell)
                .Where(spell => CanTargetRecipient(spell, targetSelf))
                .OrderByDescending(EstimatedHealPower)
                .FirstOrDefault();
        }

        private static bool IsHealingSpell(SpellDescriptor? spell)
        {
            if (spell?.Combat == null)
            {
                return false;
            }

            if (!spell.Combat.Friendly)
            {
                return false;
            }

            return spell.Combat.VitalDiff[(int)Vital.Health] > 0;
        }

        private static bool CanTargetRecipient(SpellDescriptor? spell, bool targetSelf)
        {
            if (spell?.Combat == null)
            {
                return false;
            }

            return spell.Combat.TargetType switch
            {
                SpellTargetType.Self => targetSelf,
                SpellTargetType.Single => true,
                SpellTargetType.AoE => targetSelf,
                SpellTargetType.Projectile => !targetSelf,
                SpellTargetType.OnHit => false,
                SpellTargetType.Trap => false,
                _ => false,
            };
        }

        private static long EstimatedHealPower(SpellDescriptor spell)
        {
            if (spell?.Combat == null)
            {
                return 0;
            }

            return spell.Combat.VitalDiff[(int)Vital.Health];
        }

        // ---------- Selección de Objetivo ----------
        private void UpdateTarget(long timeMs, Player owner)
        {
            if (_pet.Behavior is PetState.Passive or PetState.Stay)
            {
                _bb.CurrentTarget = null;
                return;
            }

            if (timeMs < _bb.LastRetargetTime + _cfg.RetargetIntervalMs)
            {
                return;
            }

            _bb.LastRetargetTime = timeMs;

            var now = timeMs;

            var stickMs = 1500;
            if (IsValidEnemy(_bb.CurrentTarget) && (now - _bb.CurrentTargetSince) < stickMs)
            {
                return;
            }

            if (_bb.CurrentTarget != null && IsBlacklisted(_bb.CurrentTarget, now))
            {
                _bb.CurrentTarget = null;
            }

            var recent = _bb.LastOwnerDamagedAt > 0 && (now - _bb.LastOwnerDamagedAt) < Options.Instance.Combat.CombatTime;
            if (recent && IsValidEnemy(_bb.LastOwnerAttacker) && !IsBlacklisted(_bb.LastOwnerAttacker!, now))
            {
                _bb.CurrentTarget = _bb.LastOwnerAttacker;
                _bb.CurrentTargetSince = now;
                Log($"Targeting owner's aggressor {_bb.CurrentTarget.Id}");
                return;
            }

            var ownerTarget = owner.Target;
            if (IsValidEnemy(ownerTarget) && !IsBlacklisted(ownerTarget!, now) &&
                WithinTiles(ownerTarget!.X, ownerTarget.Y, owner.X, owner.Y, _cfg.DefendTriggerRangeTiles))
            {
                _bb.CurrentTarget = ownerTarget;
                _bb.CurrentTargetSince = now;
                AddThreat(ownerTarget!, 3);
                Log($"Assisting owner on target {_bb.CurrentTarget.Id}");
                return;
            }

            var ht = HighestThreatNearOwner(owner, _cfg.ChaseRangeTiles);
            if (ht != null)
            {
                _bb.CurrentTarget = ht;
                _bb.CurrentTargetSince = now;
                Log($"Targeting highest threat {_bb.CurrentTarget.Id}");
                return;
            }

            var ownerInCombat = owner.CombatTimer > Timing.Global.Milliseconds;
            var candidates = NearbyHostiles(_cfg.ChaseRangeTiles)
                .Where(e => !IsBlacklisted(e, now))
                .OrderBy(e => TileDist(e.X, e.Y, owner.X, owner.Y))
                .ToArray();

            if (_cfg.NeverPullAggroAlone && !ownerInCombat)
            {
                candidates = candidates.Where(e => WithinTiles(e.X, e.Y, owner.X, owner.Y, 3)).ToArray();
            }

            _bb.CurrentTarget = candidates.FirstOrDefault();
            if (_bb.CurrentTarget != null)
            {
                _bb.CurrentTargetSince = now;
                Log($"Fallback targeting {_bb.CurrentTarget.Id}");
            }
        }

        private bool IsValidEnemy(Entity? e)
            => e != null && SameScene(e) && !e.IsDisposed && !e.IsDead && _pet.Owner != null && !_pet.Owner.IsAllyOf(e);

        private bool SameScene(Entity e)
            => e != null && e.MapId == _pet.MapId && e.MapInstanceId == _pet.MapInstanceId;

        private IEnumerable<Entity> NearbyHostiles(int maxTiles)
        {
            if (!MapController.TryGetInstanceFromMap(_pet.MapId, _pet.MapInstanceId, out var instance))
            {
                yield break;
            }

            foreach (var entity in instance.GetEntities())
            {
                if (entity == null || entity.IsDisposed || entity.IsDead)
                {
                    continue;
                }

                if (IsBlacklisted(entity, Timing.Global.Milliseconds))
                {
                    continue;
                }

                if (_pet.Owner != null && _pet.Owner.IsAllyOf(entity))
                {
                    continue;
                }

                if (!WithinTiles(_pet.X, _pet.Y, entity.X, entity.Y, maxTiles))
                {
                    continue;
                }

                yield return entity;
            }
        }

        // ---------- Ofensiva ----------
        private bool TryOffense()
        {
            var target = _bb.CurrentTarget;
            if (!IsValidEnemy(target))
            {
                return false;
            }

            var owner = _pet.Owner;
            if (owner == null)
            {
                return false;
            }

            // ¿tenemos maná decente?
            if (Percent(_pet.Vitals[(int)Vital.Mana], _pet.MaxVitals[(int)Vital.Mana]) < _cfg.MinManaPercentToCast)
            {
                // acercarse/mejor posición si no puede castear, o autoataque si existe
                return MoveIntoAttackRange(target);
            }

            if (IsSafeZone())
            {
                _bb.CurrentTarget = null;
                Log("In safe zone, clearing target");
                return false;
            }

            // elegir spell ofensivo
            var spell = FindBestAttackSpell(target);
            if (spell != null && !FriendlyFireRisk(spell, _pet.Owner!, target) && TryCastWithCd(spell, target))
            {
                return true;
            }

            // si no se pudo castear (rango/LOS), intenta posicionarse
            var moved = MoveIntoAttackRange(target);

            if (!moved && !_pet.IsInLineOfSight(target))
            {
                Blacklist(target, Timing.Global.Milliseconds, ms: 1000);
                Log($"Blacklist {target.Id} for LOS");
            }

            return moved;
        }

        private bool MoveIntoAttackRange(Entity target)
        {
            var preferDist = 2;
            var dist = TileDist(_pet.X, _pet.Y, target.X, target.Y);

            if (dist > preferDist)
            {
                return MoveToward(target.X, target.Y);
            }

            if (dist < 1)
            {
                return _pet.TryStepAwayFrom(target, 1);
            }

            _pet.TryFace(target);
            return false;
        }

        private SpellDescriptor? FindBestAttackSpell(Entity target)
        {
            var now = Timing.Global.Milliseconds;

            return _pet
                .GetUsableSpells()
                .Where(IsOffensiveSpell)
                .Where(s => !_spellCdUntil.TryGetValue(s.Id, out var until) || now >= until)
                .OrderByDescending(EstimatedDamage)
                .FirstOrDefault(s => InPracticalRange(s, target));
        }

        private static bool IsOffensiveSpell(SpellDescriptor? spell)
        {
            if (spell?.Combat == null)
            {
                return false;
            }

            if (spell.SpellType != SpellType.CombatSpell)
            {
                return false;
            }

            if (spell.Combat.Friendly)
            {
                return false;
            }

            return spell.Combat.VitalDiff[(int)Vital.Health] < 0 || spell.Combat.TargetType != SpellTargetType.Self;
        }

        private static long EstimatedDamage(SpellDescriptor spell)
        {
            if (spell?.Combat == null)
            {
                return 0;
            }

            var healthDiff = -spell.Combat.VitalDiff[(int)Vital.Health];
            if (healthDiff > 0)
            {
                return healthDiff;
            }

            return spell.Combat.GetEffectiveScaling(null);
        }

        private bool InPracticalRange(SpellDescriptor spell, Entity target)
        {
            if (spell?.Combat == null)
            {
                return false;
            }

            var range = spell.Combat.GetEffectiveCastRange(null);
            if (range <= 0)
            {
                range = 3;
            }

            if (!WithinTiles(_pet.X, _pet.Y, target.X, target.Y, range))
            {
                return false;
            }

            return _pet.IsInLineOfSight(target);
        }

        private bool TryCastWithCd(SpellDescriptor spell, Entity? target)
        {
            if (spell == null)
            {
                return false;
            }

            var now = Timing.Global.Milliseconds;
            if (now < _gcdUntilMs)
            {
                return false;
            }

            if (_spellCdUntil.TryGetValue(spell.Id, out var until) && now < until)
            {
                return false;
            }

            if (!_pet.CanCast(spell, out var reason))
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    Log($"Cannot cast {spell.Id}: {reason}");
                }

                return false;
            }

            var manaPct = Percent(_pet.Vitals[(int)Vital.Mana], _pet.MaxVitals[(int)Vital.Mana]);
            if (manaPct < _cfg.ManaReserveForEmergencyPercent && IsOffensiveSpell(spell))
            {
                return false;
            }

            var ok = _pet.TryCastSpell(spell, target);
            if (ok)
            {
                var cdMs = Math.Max(0, spell.CooldownDuration);
                _spellCdUntil[spell.Id] = now + cdMs;
                _gcdUntilMs = now + _cfg.GlobalCooldownMs;
                Log($"Cast {spell.Id} on {target?.Id}");
            }

            return ok;
        }

        // ---------- Movimiento utilitario ----------
        private bool MoveNearOwner(Player owner, int preferredDistance, int maxDistance)
        {
            if (WithinTiles(_pet.X, _pet.Y, owner.X, owner.Y, preferredDistance))
            {
                return false;
            }

            if (!WithinTiles(_pet.X, _pet.Y, owner.X, owner.Y, maxDistance))
            {
                return MoveToward(owner.X, owner.Y);
            }

            return MoveToward(owner.X, owner.Y);
        }

        private bool MoveToward(int tx, int ty)
        {
            var now = Timing.Global.Milliseconds;
            if (now < _bb.LastPathTime + _cfg.RepathIntervalMs)
            {
                return false;
            }

            _bb.LastPathTime = now;

            var ok = _pet.TryMoveToward(tx, ty);

            if (!ok)
            {
                _consecutivePathFails++;
                if (_bb.CurrentTarget is { } t && _consecutivePathFails >= 3)
                {
                    Blacklist(t, now, 1200);
                    _consecutivePathFails = 0;
                    Log($"Path failed, blacklisting {t.Id}");
                }
            }
            else
            {
                _consecutivePathFails = 0;
            }

            if ((_pet.X, _pet.Y) == _lastPos && _bb.CurrentTarget is { } cur)
            {
                _pet.TryStepAwayFrom(cur, 1);
            }

            _lastPos = (_pet.X, _pet.Y);

            return ok;
        }

        // ---------- Helpers geométricos ----------
        private static bool WithinTiles(int x1, int y1, int x2, int y2, int tiles)
            => TileDist(x1, y1, x2, y2) <= tiles;

        private static int TileDist(int x1, int y1, int x2, int y2)
            => Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

        private static int Percent(long cur, long max)
            => max <= 0 ? 0 : (int)((cur * 100L) / max);

        private void AddThreat(Entity e, int amount = 1)
        {
            if (e == null)
            {
                return;
            }

            if (!_bb.Threat.TryGetValue(e.Id, out var t))
            {
                t = 0;
            }

            _bb.Threat[e.Id] = Math.Clamp(t + amount, 0, 1_000_000);
        }

        private bool IsBlacklisted(Entity e, long now)
            => _bb.BlacklistUntil.TryGetValue(e.Id, out var until) && now < until;

        private void Blacklist(Entity e, long now, int ms = 2000)
        {
            _bb.BlacklistUntil[e.Id] = now + ms;
        }

        private Entity? HighestThreatNearOwner(Player owner, int maxTiles)
        {
            if (!MapController.TryGetInstanceFromMap(_pet.MapId, _pet.MapInstanceId, out var inst))
            {
                return null;
            }

            var now = Timing.Global.Milliseconds;

            return inst.GetEntities()
                .Where(e => IsValidEnemy(e) && !IsBlacklisted(e!, now))
                .Where(e => WithinTiles(e.X, e.Y, owner.X, owner.Y, maxTiles))
                .OrderByDescending(e => _bb.Threat.TryGetValue(e.Id, out var t) ? t : 0)
                .ThenBy(e => TileDist(e.X, e.Y, owner.X, owner.Y))
                .FirstOrDefault();
        }

        private bool IsSafeZone()
        {
            var controller = MapController.Get(_pet.MapId);
            return controller?.ZoneType == MapZone.Safe;
        }

        private bool FriendlyFireRisk(SpellDescriptor s, Player owner, Entity target)
        {
            if (s?.Combat == null)
            {
                return false;
            }

            if (s.Combat.Friendly)
            {
                return false;
            }

            var range = s.Combat.GetEffectiveCastRange(null);
            if (range <= 0)
            {
                range = 1;
            }

            if (s.Combat.TargetType is SpellTargetType.AoE or SpellTargetType.Projectile)
            {
                if (WithinTiles(owner.X, owner.Y, target.X, target.Y, Math.Max(1, range)))
                {
                    return true;
                }

                if (MapController.TryGetInstanceFromMap(_pet.MapId, _pet.MapInstanceId, out var inst))
                {
                    foreach (var entity in inst.GetEntities())
                    {
                        if (entity == null || entity.IsDisposed || entity.IsDead)
                        {
                            continue;
                        }

                        if (!owner.IsAllyOf(entity))
                        {
                            continue;
                        }

                        if (WithinTiles(entity.X, entity.Y, target.X, target.Y, Math.Max(1, range)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool PanicFleeIfNecessary(Player owner)
        {
            var hpPct = Percent(_pet.Vitals[(int)Vital.Health], _pet.MaxVitals[(int)Vital.Health]);
            if (hpPct >= _cfg.PanicFleeBelowSelfPercent)
            {
                return false;
            }

            var shield = _pet.GetUsableSpells().FirstOrDefault(IsDefensiveBuff);
            if (shield != null && TryCastWithCd(shield, _pet as Entity))
            {
                Log("Panic defensive buff");
                return true;
            }

            if (MoveToward(owner.X, owner.Y))
            {
                Log("Panic flee toward owner");
                return true;
            }

            if (_bb.CurrentTarget != null && _pet.TryStepAwayFrom(_bb.CurrentTarget, _cfg.PanicFleeTiles))
            {
                Log("Panic step away from target");
                return true;
            }

            return false;
        }

        private static bool IsDefensiveBuff(SpellDescriptor s)
        {
            return s?.Combat != null && s.Combat.Friendly && s.Combat.TargetType == SpellTargetType.Self &&
                   (s.Combat.ArmorDiff > 0 || s.Combat.ResistDiff > 0 || s.Combat.BarrierAmount > 0);
        }

        private void Log(string msg)
        {
            OnDebug?.Invoke($"[PetAI {_pet.Id}] {msg}");
        }
    }
}
