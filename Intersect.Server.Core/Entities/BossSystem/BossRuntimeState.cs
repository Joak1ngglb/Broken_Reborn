namespace Intersect.Server.Entities.BossSystem;

public sealed class BossRuntimeState
{
    public Guid NpcId { get; init; }

    public bool IsAlive { get; set; }

    public long? NextRespawnAt { get; set; }

    public Guid? AliveEntityId { get; set; }

    public Guid MapId { get; set; }

    public Guid MapInstanceId { get; set; }
}
