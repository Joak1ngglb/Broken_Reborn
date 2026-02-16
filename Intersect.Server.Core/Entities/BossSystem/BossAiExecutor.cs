using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Server.Entities;
using Intersect.Utilities;

namespace Intersect.Server.Entities.BossSystem;

internal sealed class BossAiExecutor
{
    private readonly BossAiRuntimeState _runtime = new();

    public bool TryExecute(Npc npc, long now)
    {
        if (npc.Descriptor.BossBehaviorProfile is not { Phases.Count: > 0 } profile)
        {
            return false;
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
        var candidates = BuildCandidates(npc, profile, target, now);
        if (candidates.Count == 0)
        {
            return false;
        }

        var selected = candidates
            .OrderByDescending(c => c.Priority)
            .ThenBy(c => c.Index)
            .First();

        if (!TryExecuteAction(npc, selected.Action, target, now))
        {
            return false;
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

    private List<ActionCandidate> BuildCandidates(Npc npc, BossBehaviorProfile profile, Entity target, long now)
    {
        var candidates = new List<ActionCandidate>();
        var healthPercent = GetHealthPercent(npc);
        var runningIndex = 0;

        foreach (var phase in profile.Phases.OrderByDescending(p => p.Priority))
        {
            if (healthPercent < phase.MinimumHealthPercent || healthPercent > phase.MaximumHealthPercent)
            {
                continue;
            }

            foreach (var action in phase.Actions)
            {
                if (IsActionOnCooldown(action, now))
                {
                    runningIndex++;
                    continue;
                }

                candidates.Add(new ActionCandidate(action, phase.Priority + action.Priority, runningIndex++, 0, string.Empty));
            }

            for (var triggerIndex = 0; triggerIndex < phase.Triggers.Count; triggerIndex++)
            {
                var trigger = phase.Triggers[triggerIndex];
                var triggerCooldownKey = $"{phase.Id}:{triggerIndex}";
                if (trigger.CooldownSeconds > 0 &&
                    _runtime.TriggerCooldowns.TryGetValue(triggerCooldownKey, out var triggerCooldownUntil) &&
                    triggerCooldownUntil > now)
                {
                    runningIndex += trigger.Actions.Count;
                    continue;
                }

                if (!IsTriggerActive(npc, trigger, target, now))
                {
                    runningIndex += trigger.Actions.Count;
                    continue;
                }

                foreach (var action in trigger.Actions)
                {
                    if (IsActionOnCooldown(action, now))
                    {
                        runningIndex++;
                        continue;
                    }

                    var priority = phase.Priority + trigger.Priority + action.Priority;
                    candidates.Add(
                        new ActionCandidate(
                            action,
                            priority,
                            runningIndex++,
                            trigger.CooldownSeconds * 1000,
                            triggerCooldownKey
                        )
                    );
                }
            }
        }

        return candidates;
    }

    private bool IsTriggerActive(Npc npc, BossTrigger trigger, Entity target, long now)
    {
        if (!EvaluateLegacyTriggerCondition(npc, trigger, target, now))
        {
            return false;
        }

        foreach (var condition in trigger.Conditions)
        {
            if (!EvaluateCondition(npc, condition, target, now))
            {
                return false;
            }
        }

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
        switch (action.Action)
        {
            case BossActionType.CastSpell:
                if (action.SpellId == Guid.Empty)
                {
                    return false;
                }

                if (npc.TryCastSpellById(action.SpellId, target))
                {
                    _runtime.LastCastAt = now;
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

                    _runtime.LastCastAt = now;
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

    private static int GetHealthPercent(Entity entity)
    {
        var maxHealth = entity.GetMaxVital(Vital.Health);
        if (maxHealth <= 0)
        {
            return 0;
        }

        return (int)Math.Clamp(entity.GetVital(Vital.Health) * 100 / maxHealth, 0, 100);
    }

    private readonly record struct ActionCandidate(
        BossAction Action,
        int Priority,
        int Index,
        int TriggerCooldownMs,
        string TriggerCooldownKey
    );
}
