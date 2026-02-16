using System.Collections.Concurrent;

namespace Intersect.Server.Entities.BossSystem;

internal sealed class BossAiRuntimeState
{
    public long LastEvaluationAt { get; set; }

    public long GlobalCooldownUntil { get; set; }

    public long LastCastAt { get; set; }

    public ConcurrentDictionary<string, long> ActionCooldowns { get; } = new();

    public ConcurrentDictionary<string, bool> InternalStates { get; } = new();

    public ConcurrentDictionary<string, long> TriggerCooldowns { get; } = new();
}
