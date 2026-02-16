using System;
using System.Collections.Generic;

using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Server.Entities.BossSystem;
using NUnit.Framework;

namespace Intersect.Tests.Server.Entities;

[TestFixture]
public class BossAiDecisionSelectorTests
{
    [Test]
    public void Hp30PercentTrigger_SelectsExpectedAction()
    {
        var selector = new BossAiDecisionSelector();
        var expectedSpell = Guid.NewGuid();
        var profile = new BossBehaviorProfile
        {
            Phases =
            [
                new BossPhase
                {
                    Id = "phase-1",
                    MinimumHealthPercent = 0,
                    MaximumHealthPercent = 100,
                    Priority = 1,
                    Triggers =
                    [
                        new BossTrigger
                        {
                            Condition = BossTriggerConditionType.HealthPercentAtOrBelow,
                            ThresholdHealthPercent = 30,
                            Priority = 50,
                            Actions =
                            [
                                new BossAction
                                {
                                    Action = BossActionType.CastSpell,
                                    SpellId = expectedSpell,
                                    Priority = 20,
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        var result = selector.BuildCandidates(
            profile,
            30,
            now: 0,
            isTriggerActive: (_, trigger, _) => trigger.ThresholdHealthPercent >= 30,
            canUseAction: _ => true,
            isActionOnCooldown: _ => false
        );

        Assert.That(result.Selected, Is.Not.Null);
        Assert.That(result.Selected!.Action.SpellId, Is.EqualTo(expectedSpell));
    }

    [Test]
    public void SpellOnCooldown_IsNotSelected()
    {
        var selector = new BossAiDecisionSelector();
        var onCooldownSpell = Guid.NewGuid();
        var availableSpell = Guid.NewGuid();
        var profile = new BossBehaviorProfile
        {
            Phases =
            [
                new BossPhase
                {
                    Id = "phase-1",
                    Priority = 1,
                    Actions =
                    [
                        new BossAction { Action = BossActionType.CastSpell, SpellId = onCooldownSpell, Priority = 100 },
                        new BossAction { Action = BossActionType.CastSpell, SpellId = availableSpell, Priority = 10 },
                    ],
                },
            ],
        };

        var result = selector.BuildCandidates(
            profile,
            80,
            now: 0,
            isTriggerActive: (_, _, _) => false,
            canUseAction: _ => true,
            isActionOnCooldown: action => action.SpellId == onCooldownSpell
        );

        Assert.That(result.Selected, Is.Not.Null);
        Assert.That(result.Selected!.Action.SpellId, Is.EqualTo(availableSpell));
    }

    [Test]
    public void PhaseChanges_WhenCrossingHealthThreshold()
    {
        var selector = new BossAiDecisionSelector();
        var highPhaseSpell = Guid.NewGuid();
        var lowPhaseSpell = Guid.NewGuid();
        var profile = new BossBehaviorProfile
        {
            Phases =
            [
                new BossPhase
                {
                    Id = "high",
                    MinimumHealthPercent = 51,
                    MaximumHealthPercent = 100,
                    Priority = 10,
                    Actions = [ new BossAction { Action = BossActionType.CastSpell, SpellId = highPhaseSpell, Priority = 10 } ],
                },
                new BossPhase
                {
                    Id = "low",
                    MinimumHealthPercent = 0,
                    MaximumHealthPercent = 50,
                    Priority = 20,
                    Actions = [ new BossAction { Action = BossActionType.CastSpell, SpellId = lowPhaseSpell, Priority = 10 } ],
                },
            ],
        };

        var firstEval = selector.BuildCandidates(profile, 70, 0, (_, _, _) => false, _ => true, _ => false);
        var secondEval = selector.BuildCandidates(profile, 45, 1000, (_, _, _) => false, _ => true, _ => false);

        Assert.That(firstEval.Selected, Is.Not.Null);
        Assert.That(firstEval.Selected!.PhaseId, Is.EqualTo("high"));
        Assert.That(secondEval.Selected, Is.Not.Null);
        Assert.That(secondEval.Selected!.PhaseId, Is.EqualTo("low"));
    }

    [Test]
    public void Integration_TwoPhaseCombatFlow_TransitionsAndRespectsCooldowns()
    {
        var selector = new BossAiDecisionSelector();
        var phase1Spell = Guid.NewGuid();
        var phase2Spell = Guid.NewGuid();
        var profile = new BossBehaviorProfile
        {
            Phases =
            [
                new BossPhase
                {
                    Id = "phase1",
                    MinimumHealthPercent = 51,
                    MaximumHealthPercent = 100,
                    Priority = 10,
                    Triggers =
                    [
                        new BossTrigger
                        {
                            Priority = 50,
                            CooldownSeconds = 3,
                            Actions = [ new BossAction { Action = BossActionType.CastSpell, SpellId = phase1Spell, Priority = 10 } ],
                        },
                    ],
                },
                new BossPhase
                {
                    Id = "phase2",
                    MinimumHealthPercent = 0,
                    MaximumHealthPercent = 50,
                    Priority = 20,
                    Actions = [ new BossAction { Action = BossActionType.CastSpell, SpellId = phase2Spell, Priority = 10 } ],
                },
            ],
        };

        var triggerCooldowns = new Dictionary<string, long>();
        var timeline = new[]
        {
            (now: 0L, hp: 100),
            (now: 1000L, hp: 95),
            (now: 3500L, hp: 48),
        };

        var selectedPhases = new List<string>();
        foreach (var tick in timeline)
        {
            var result = selector.BuildCandidates(
                profile,
                tick.hp,
                tick.now,
                (phaseId, trigger, triggerIndex) =>
                {
                    var key = $"{phaseId}:{triggerIndex}";
                    return !triggerCooldowns.TryGetValue(key, out var until) || until <= tick.now;
                },
                _ => true,
                _ => false
            );

            if (result.Selected == null)
            {
                continue;
            }

            selectedPhases.Add(result.Selected.PhaseId);
            if (result.Selected.TriggerCooldownMs > 0)
            {
                triggerCooldowns[result.Selected.TriggerCooldownKey] = tick.now + result.Selected.TriggerCooldownMs;
            }
        }

        Assert.That(selectedPhases, Is.EqualTo(new[] { "phase1", "phase2" }));
    }
}
