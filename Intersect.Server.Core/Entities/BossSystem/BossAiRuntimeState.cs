using System.Collections.Concurrent;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Server.Entities;

namespace Intersect.Server.Entities.BossSystem;

internal sealed class BossAiRuntimeState
{
    public sealed class PendingBossAction
    {
        public required BossAction Action { get; init; }

        public required Entity? Target { get; init; }

        public required long ExecuteAt { get; init; }
    }

    public long LastEvaluationAt { get; set; }

    public long GlobalCooldownUntil { get; set; }

    public long LastCastAt { get; set; }

    public long LastBigSkillAt { get; set; }

    public int ConsecutiveControlCasts { get; set; }

    public string CurrentPhaseId { get; set; } = string.Empty;

    public PendingBossAction? PendingAction { get; set; }

    public ConcurrentDictionary<string, long> ActionCooldowns { get; } = new();

    public ConcurrentDictionary<string, bool> InternalStates { get; } = new();

    public ConcurrentDictionary<string, long> TriggerCooldowns { get; } = new();
}
