using System.Linq;
using Intersect.Framework.Core.GameObjects.NPCs;

namespace Intersect.Server.Entities.BossSystem;

internal sealed class BossAiDecisionSelector
{
    public BossAiSelectionResult BuildCandidates(
        BossBehaviorProfile profile,
        int healthPercent,
        long now,
        Func<string, BossTrigger, int, bool> isTriggerActive,
        Func<BossAction, bool> canUseAction,
        Func<BossAction, bool> isActionOnCooldown,
        Action<string>? debugLog = null
    )
    {
        var candidates = new List<BossAiActionCandidate>();
        var runningIndex = 0;

        foreach (var phase in profile.Phases.OrderByDescending(p => p.Priority))
        {
            var inRange = healthPercent >= phase.MinimumHealthPercent && healthPercent <= phase.MaximumHealthPercent;
            if (!inRange)
            {
                debugLog?.Invoke(
                    $"Fase '{phase.Id}' descartada por umbral de vida. hp={healthPercent}% rango={phase.MinimumHealthPercent}-{phase.MaximumHealthPercent}%"
                );
                continue;
            }

            debugLog?.Invoke($"Fase '{phase.Id}' activa para hp={healthPercent}%.");

            foreach (var action in phase.Actions)
            {
                if (!canUseAction(action))
                {
                    debugLog?.Invoke($"Acción {action.Action} descartada en fase '{phase.Id}': restricciones de uso.");
                    runningIndex++;
                    continue;
                }

                if (isActionOnCooldown(action))
                {
                    debugLog?.Invoke($"Acción {action.Action} descartada en fase '{phase.Id}': cooldown activo.");
                    runningIndex++;
                    continue;
                }

                candidates.Add(new BossAiActionCandidate(action, phase.Id, phase.Priority + action.Priority, runningIndex++, 0, string.Empty));
            }

            for (var triggerIndex = 0; triggerIndex < phase.Triggers.Count; triggerIndex++)
            {
                var trigger = phase.Triggers[triggerIndex];
                var triggerCooldownKey = $"{phase.Id}:{triggerIndex}";
                if (!isTriggerActive(phase.Id, trigger, triggerIndex))
                {
                    debugLog?.Invoke($"Trigger evaluado en fase '{phase.Id}' idx={triggerIndex}: inactivo.");
                    runningIndex += trigger.Actions.Count;
                    continue;
                }

                debugLog?.Invoke($"Trigger evaluado en fase '{phase.Id}' idx={triggerIndex}: activo.");

                foreach (var action in trigger.Actions)
                {
                    if (!canUseAction(action))
                    {
                        debugLog?.Invoke($"Acción {action.Action} de trigger {triggerIndex} descartada: restricciones de uso.");
                        runningIndex++;
                        continue;
                    }

                    if (isActionOnCooldown(action))
                    {
                        debugLog?.Invoke($"Acción {action.Action} de trigger {triggerIndex} descartada: cooldown activo.");
                        runningIndex++;
                        continue;
                    }

                    var priority = phase.Priority + trigger.Priority + action.Priority;
                    candidates.Add(
                        new BossAiActionCandidate(
                            action,
                            phase.Id,
                            priority,
                            runningIndex++,
                            trigger.CooldownSeconds * 1000,
                            triggerCooldownKey
                        )
                    );
                }
            }
        }

        var selected = candidates
            .OrderByDescending(c => c.Priority)
            .ThenBy(c => c.Index)
            .FirstOrDefault();

        if (selected != null)
        {
            debugLog?.Invoke(
                $"Acción elegida: {selected.Action.Action} (fase='{selected.PhaseId}', prioridad={selected.Priority}, índice={selected.Index})."
            );
        }
        else
        {
            debugLog?.Invoke("No se seleccionó ninguna acción de IA para esta evaluación.");
        }

        return new BossAiSelectionResult(candidates, selected);
    }
}

internal sealed record BossAiActionCandidate(
    BossAction Action,
    string PhaseId,
    int Priority,
    int Index,
    int TriggerCooldownMs,
    string TriggerCooldownKey
);

internal sealed record BossAiSelectionResult(
    IReadOnlyList<BossAiActionCandidate> Candidates,
    BossAiActionCandidate? Selected
);
