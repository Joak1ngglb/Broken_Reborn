using System.Linq;
using Intersect;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Server.Entities;
using Intersect.Server.Networking;
using Intersect.Utilities;

namespace Intersect.Server.Entities.BossSystem;

internal sealed class BossAiExecutor
{
    private readonly BossAiRuntimeState _runtime = new();
    private readonly BossAiDecisionSelector _selector = new();

    public bool TryExecute(Npc npc, long now)
    {
        if (npc.Descriptor.BossBehaviorProfile is not { Phases.Count: > 0 } profile)
        {
            return false;
        }

        if (TryExecutePendingAction(npc, now))
        {
            return true;
        }

        var evaluationInterval = Math.Max(100, profile.EvaluationIntervalMs);
        if (_runtime.LastEvaluationAt + evaluationInterval > now)
        {
            return false;
        }

        _runtime.LastEvaluationAt = now;

        if (_runtime.GlobalCooldownUntil > now)
        {
            return false;
        }

        var target = npc.Target;
        var bossId = npc.Descriptor.Id;
        var debugEnabled = BossAiDebugSettings.IsEnabled(bossId);
        var selection = BuildCandidates(npc, profile, target, now, debugEnabled);
        if (selection.Candidates.Count == 0 || selection.Selected == null)
        {
            return TryFallbackAction(npc, target);
        }

        var selected = selection.Selected;
        if (!string.Equals(_runtime.CurrentPhaseId, selected.PhaseId, StringComparison.Ordinal))
        {
            LogDebug(
                debugEnabled,
                npc,
                "Cambio de fase detectado: '{PreviousPhase}' -> '{NextPhase}'.",
                string.IsNullOrWhiteSpace(_runtime.CurrentPhaseId) ? "<none>" : _runtime.CurrentPhaseId,
                selected.PhaseId
            );
            _runtime.CurrentPhaseId = selected.PhaseId;
        }

        if (ShouldTelegraphAction(selected.Action, profile) &&
            TryStartTelegraph(npc, selected.Action, target, profile, now))
        {
            return true;
        }

        if (!TryExecuteAction(npc, selected.Action, target, now))
        {
            return TryFallbackAction(npc, target);
        }

        var actionKey = BuildActionCooldownKey(selected.Action);
        if (selected.Action.CooldownMs > 0)
        {
            _runtime.ActionCooldowns[actionKey] = now + selected.Action.CooldownMs;
        }

        if (selected.TriggerCooldownMs > 0)
        {
            _runtime.TriggerCooldowns[selected.TriggerCooldownKey] = now + selected.TriggerCooldownMs;
        }

        var globalCooldownMs = selected.Action.CooldownMs > 0
            ? Math.Max(profile.GlobalCooldownMs, selected.Action.CooldownMs)
            : profile.GlobalCooldownMs;

        if (globalCooldownMs > 0)
        {
            _runtime.GlobalCooldownUntil = now + globalCooldownMs;
        }

        return true;
    }

    private BossAiSelectionResult BuildCandidates(Npc npc, BossBehaviorProfile profile, Entity target, long now, bool debugEnabled)
    {
        var healthPercent = GetHealthPercent(npc);
        return _selector.BuildCandidates(
            profile,
            healthPercent,
            now,
            (phaseId, trigger, triggerIndex) =>
            {
                var triggerCooldownKey = $"{phaseId}:{triggerIndex}";
                if (trigger.CooldownSeconds > 0 &&
                    _runtime.TriggerCooldowns.TryGetValue(triggerCooldownKey, out var triggerCooldownUntil) &&
                    triggerCooldownUntil > now)
                {
                    LogDebug(debugEnabled, npc, "Trigger evaluado idx={TriggerIndex}: en cooldown hasta {CooldownUntil}.", triggerIndex, triggerCooldownUntil);
                    return false;
                }

                return IsTriggerActive(npc, trigger, target, now, debugEnabled, triggerIndex);
            },
            action => CanUseAction(action, profile, now),
            action => IsActionOnCooldown(action, now),
            message => LogDebug(debugEnabled, npc, message)
        );
    }

    private bool IsTriggerActive(Npc npc, BossTrigger trigger, Entity target, long now, bool debugEnabled, int triggerIndex)
    {
        if (!EvaluateLegacyTriggerCondition(npc, trigger, target, now))
        {
            LogDebug(debugEnabled, npc, "Trigger evaluado idx={TriggerIndex}: condición legacy no cumplida.", triggerIndex);
            return false;
        }

        foreach (var condition in trigger.Conditions)
        {
            if (!EvaluateCondition(npc, condition, target, now))
            {
                LogDebug(debugEnabled, npc, "Trigger evaluado idx={TriggerIndex}: condición {ConditionType} no cumplida.", triggerIndex, condition.Type);
                return false;
            }
        }

        LogDebug(debugEnabled, npc, "Trigger evaluado idx={TriggerIndex}: activo.", triggerIndex);
        return true;
    }

    private static bool EvaluateLegacyTriggerCondition(Npc npc, BossTrigger trigger, Entity target, long now)
    {
        return trigger.Condition switch
        {
            BossTriggerConditionType.HealthPercentAtOrBelow =>
                trigger.ThresholdHealthPercent <= 0 || GetHealthPercent(npc) <= trigger.ThresholdHealthPercent,
            BossTriggerConditionType.StatusApplied =>
                string.IsNullOrWhiteSpace(trigger.RequiredAppliedState) || HasStatusByName(target, trigger.RequiredAppliedState),
            BossTriggerConditionType.CombatTimeAtOrAbove =>
                trigger.CombatTimeSeconds <= 0 || now >= npc.CombatTimer + trigger.CombatTimeSeconds * 1000L,
            _ => true,
        };
    }

    private bool EvaluateCondition(Npc npc, BossCondition condition, Entity target, long now)
    {
        switch (condition.Type)
        {
            case BossConditionType.SelfHealthPercent:
                return Compare(GetHealthPercent(npc), condition);
            case BossConditionType.TargetHealthPercent:
                return target != null && Compare(GetHealthPercent(target), condition);
            case BossConditionType.StatusPresent:
            {
                var statusTarget = condition.StatusTarget == BossStatusTargetType.CurrentTarget ? target : npc;
                if (statusTarget == null)
                {
                    return false;
                }

                return statusTarget.HasStatusEffect(condition.StatusEffect);
            }
            case BossConditionType.DistanceToTarget:
                return target != null && Compare(npc.GetDistanceTo(target), condition);
            case BossConditionType.TimeSinceLastCastMilliseconds:
            {
                var elapsed = Math.Max(0, now - _runtime.LastCastAt);
                return Compare(elapsed, condition);
            }
            case BossConditionType.InternalState:
                return _runtime.InternalStates.TryGetValue(condition.InternalStateKey ?? string.Empty, out var state) && state;
            default:
                return false;
        }
    }

    private static bool Compare(long actualValue, BossCondition condition)
    {
        return condition.Comparison switch
        {
            BossComparisonType.LessOrEqual => actualValue <= condition.Value,
            BossComparisonType.GreaterOrEqual => actualValue >= condition.Value,
            BossComparisonType.BetweenInclusive => actualValue >= condition.MinimumValue && actualValue <= condition.MaximumValue,
            _ => false,
        };
    }

    private bool TryExecuteAction(Npc npc, BossAction action, Entity target, long now)
    {
        if (!CanUseAction(action, npc.Descriptor.BossBehaviorProfile, now))
        {
            return false;
        }

        switch (action.Action)
        {
            case BossActionType.CastSpell:
                if (action.SpellId == Guid.Empty)
                {
                    return false;
                }

                if (npc.TryCastSpellById(action.SpellId, target))
                {
                    OnSuccessfulCast(action, now);
                    return true;
                }

                return false;
            case BossActionType.CastSpellFromList:
            {
                var spellIds = action.SpellIds?.Where(id => id != Guid.Empty).ToArray();
                if (spellIds is not { Length: > 0 })
                {
                    return false;
                }

                var startIndex = Randomization.Next(0, spellIds.Length);
                for (var index = 0; index < spellIds.Length; index++)
                {
                    var spellId = spellIds[(startIndex + index) % spellIds.Length];
                    if (!npc.TryCastSpellById(spellId, target))
                    {
                        continue;
                    }

                    OnSuccessfulCast(action, now);
                    return true;
                }

                return false;
            }
            case BossActionType.ChangeTarget:
                if (TrySelectTarget(npc, action.TargetSelection, out var selectedTarget))
                {
                    npc.AssignTarget(selectedTarget);
                    return true;
                }

                return false;
            case BossActionType.Enrage:
            case BossActionType.SetInternalState:
                if (string.IsNullOrWhiteSpace(action.InternalStateKey))
                {
                    return false;
                }

                _runtime.InternalStates[action.InternalStateKey] = action.InternalStateValue;
                return true;
            default:
                return false;
        }
    }

    private static bool TrySelectTarget(Npc npc, BossTargetSelectionType selection, out Entity selected)
    {
        selected = null;
        var candidates = npc.DamageMap.Keys
            .Where(entity => entity != null && !entity.IsDisposed && !entity.IsDead && npc.CanTarget(entity))
            .ToArray();

        if (candidates.Length == 0)
        {
            return false;
        }

        selected = selection switch
        {
            BossTargetSelectionType.HighestThreat => npc.DamageMapHighest,
            BossTargetSelectionType.Tank => candidates.OrderByDescending(entity => entity.GetMaxVital(Vital.Health)).FirstOrDefault(),
            BossTargetSelectionType.Healer => candidates.OrderByDescending(entity => entity.Stat[(int)Stat.Intelligence].Value()).FirstOrDefault(),
            _ => npc.DamageMapHighest,
        };

        if (selected != null)
        {
            return true;
        }

        selected = candidates[0];
        return true;
    }

    private bool IsActionOnCooldown(BossAction action, long now)
    {
        var actionKey = BuildActionCooldownKey(action);
        return _runtime.ActionCooldowns.TryGetValue(actionKey, out var actionCooldownUntil) && actionCooldownUntil > now;
    }

    private static string BuildActionCooldownKey(BossAction action)
    {
        return action.Action switch
        {
            BossActionType.CastSpell => $"cast:{action.SpellId}",
            BossActionType.CastSpellFromList => $"cast-list:{string.Join(',', action.SpellIds?.Where(id => id != Guid.Empty) ?? Enumerable.Empty<Guid>())}",
            BossActionType.ChangeTarget => $"target:{action.TargetSelection}",
            BossActionType.SetInternalState or BossActionType.Enrage => $"state:{action.InternalStateKey}",
            _ => action.Action.ToString(),
        };
    }

    private static bool HasStatusByName(Entity target, string statusName)
    {
        if (target == null || string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        if (!Enum.TryParse<SpellEffect>(statusName, true, out var spellEffect))
        {
            return false;
        }

        return target.HasStatusEffect(spellEffect);
    }

    private bool CanUseAction(BossAction action, BossBehaviorProfile profile, long now)
    {
        profile ??= new BossBehaviorProfile();

        if (IsControlAction(action) &&
            profile.MaxConsecutiveControlCasts > 0 &&
            _runtime.ConsecutiveControlCasts >= profile.MaxConsecutiveControlCasts)
        {
            return false;
        }

        if (IsBigSkill(action) &&
            profile.MinIntervalBetweenBigSkills > 0 &&
            _runtime.LastBigSkillAt + profile.MinIntervalBetweenBigSkills > now)
        {
            return false;
        }

        return true;
    }

    private void OnSuccessfulCast(BossAction action, long now)
    {
        _runtime.LastCastAt = now;
        _runtime.ConsecutiveControlCasts = IsControlAction(action) ? _runtime.ConsecutiveControlCasts + 1 : 0;

        if (IsBigSkill(action))
        {
            _runtime.LastBigSkillAt = now;
        }
    }

    private static bool IsControlAction(BossAction action)
    {
        return action.IsControlSkill;
    }

    private static bool IsBigSkill(BossAction action)
    {
        return action.IsBigSkill;
    }

    private bool TryExecutePendingAction(Npc npc, long now)
    {
        var pending = _runtime.PendingAction;
        if (pending == null)
        {
            return false;
        }

        if (pending.ExecuteAt > now)
        {
            return true;
        }

        _runtime.PendingAction = null;
        if (TryExecuteAction(npc, pending.Action, pending.Target, now))
        {
            return true;
        }

        return TryFallbackAction(npc, npc.Target);
    }

    private bool TryStartTelegraph(Npc npc, BossAction action, Entity target, BossBehaviorProfile profile, long now)
    {
        if (profile.TelegraphMs <= 0)
        {
            return false;
        }

        var telegraphMessage = string.IsNullOrWhiteSpace(action.TelegraphMessage)
            ? $"{npc.Name} prepara una habilidad crítica..."
            : action.TelegraphMessage;

        PacketSender.SendActionMsg(npc, telegraphMessage, new Color(255, 255, 215, 0));
        PacketSender.SendChatBubble(npc.Id, npc.MapInstanceId, EntityType.GlobalEntity, telegraphMessage, npc.MapId);

        var telegraphAnimation = action.TelegraphAnimationId;
        if (telegraphAnimation != Guid.Empty)
        {
            PacketSender.SendAnimationToProximity(
                telegraphAnimation,
                1,
                npc.Id,
                npc.MapId,
                0,
                0,
                npc.Dir,
                npc.MapInstanceId,
                AnimationSourceType.SpellCast,
                Guid.Empty
            );
        }

        var executeAt = now + profile.TelegraphMs;
        _runtime.PendingAction = new BossAiRuntimeState.PendingBossAction
        {
            Action = action,
            Target = target,
            ExecuteAt = executeAt,
        };

        _runtime.GlobalCooldownUntil = Math.Max(_runtime.GlobalCooldownUntil, executeAt);
        return true;
    }

    private static bool ShouldTelegraphAction(BossAction action, BossBehaviorProfile profile)
    {
        return action.IsCriticalSkill && profile.TelegraphMs > 0;
    }

    private static bool TryFallbackAction(Npc npc, Entity target)
    {
        if (target != null && !target.IsDead && npc.CanAttack(target))
        {
            npc.TryAttack(target);
            return true;
        }

        var directions = new[]
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right,
            Direction.UpLeft,
            Direction.UpRight,
            Direction.DownRight,
            Direction.DownLeft,
        };

        var proposedDirection = directions[Randomization.Next(0, directions.Length)];
        return npc.TryMove(proposedDirection, true, true);
    }

    private static int GetHealthPercent(Entity entity)
    {
        var maxHealth = entity.GetMaxVital(Vital.Health);
        if (maxHealth <= 0)
        {
            return 0;
        }

        return (int)Math.Clamp(entity.GetVital(Vital.Health) * 100 / maxHealth, 0, 100);
    }

    private static void LogDebug(bool enabled, Npc npc, string message, params object[] args)
    {
        if (!enabled)
        {
            return;
        }

        var logArgs = new object[args.Length + 2];
        logArgs[0] = npc.Name;
        logArgs[1] = npc.Descriptor.Id;
        Array.Copy(args, 0, logArgs, 2, args.Length);

        ApplicationContext.Context.Value?.Logger.LogDebug(
            "[BossAI:{BossName}/{BossId}] " + message,
            logArgs
        );
    }
}
