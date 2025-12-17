using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public sealed class StartFishingPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public StartFishingPacket()
    {
    }

    public StartFishingPacket(Guid fishId, long stageTimer, long resolveTimer, FishingStage stage, bool cancelRequested)
    {
        FishId = fishId;
        StageTimer = stageTimer;
        ResolveTimer = resolveTimer;
        Stage = stage;
        CancelRequested = cancelRequested;
    }

    [Key(0)]
    public Guid FishId { get; set; }

    [Key(1)]
    public long StageTimer { get; set; }

    [Key(2)]
    public long ResolveTimer { get; set; }

    [Key(3)]
    public FishingStage Stage { get; set; }

    [Key(4)]
    public bool CancelRequested { get; set; }
}

[MessagePackObject]
public sealed class ResolveFishingPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public ResolveFishingPacket()
    {
    }

    public ResolveFishingPacket(Guid fishId, long resolveTimer, bool cancelRequested, bool canceled)
    {
        FishId = fishId;
        ResolveTimer = resolveTimer;
        CancelRequested = cancelRequested;
        Canceled = canceled;
    }

    [Key(0)]
    public Guid FishId { get; set; }

    [Key(1)]
    public long ResolveTimer { get; set; }

    [Key(2)]
    public bool CancelRequested { get; set; }

    [Key(3)]
    public bool Canceled { get; set; }
}

[MessagePackObject]
public sealed class StopFishingPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public StopFishingPacket()
    {
    }

    public StopFishingPacket(bool canceled)
    {
        Canceled = canceled;
    }

    [Key(0)]
    public bool Canceled { get; set; }
}
