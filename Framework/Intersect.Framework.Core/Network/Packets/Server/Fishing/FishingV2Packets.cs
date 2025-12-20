using System;
using Intersect.Fishing;
using MessagePack;

namespace Intersect.Network.Packets.Server.Fishing;

[MessagePackObject]
public sealed class FishingCastResult : IntersectPacket
{
    public FishingCastResult()
    {
    }

    public FishingCastResult(Guid sessionId, bool success, string? reason = null)
    {
        SessionId = sessionId;
        Success = success;
        Reason = reason;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public bool Success { get; set; }

    [Key(2)]
    public string? Reason { get; set; }
}

[MessagePackObject]
public sealed class FishingBiteStart : IntersectPacket
{
    public FishingBiteStart()
    {
    }

    public FishingBiteStart(
        Guid sessionId,
        Guid fishId,
        FishingSimConfigDto config,
        FishingSimStateDto state,
        uint rngState,
        long stageTimer,
        long resolveTimer)
    {
        SessionId = sessionId;
        FishId = fishId;
        Config = config;
        State = state;
        RngState = rngState;
        StageTimer = stageTimer;
        ResolveTimer = resolveTimer;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public Guid FishId { get; set; }

    [Key(2)]
    public FishingSimConfigDto Config { get; set; }

    [Key(3)]
    public FishingSimStateDto State { get; set; }

    [Key(4)]
    public uint RngState { get; set; }

    [Key(5)]
    public long StageTimer { get; set; }

    [Key(6)]
    public long ResolveTimer { get; set; }
}

[MessagePackObject]
public sealed class FishingStateSnapshot : IntersectPacket
{
    public FishingStateSnapshot()
    {
    }

    public FishingStateSnapshot(Guid sessionId, FishingSimStateDto state, uint rngState)
    {
        SessionId = sessionId;
        State = state;
        RngState = rngState;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public FishingSimStateDto State { get; set; }

    [Key(2)]
    public uint RngState { get; set; }
}

[MessagePackObject]
public sealed class FishingResolve : IntersectPacket
{
    public FishingResolve()
    {
    }

    public FishingResolve(Guid sessionId, Guid fishId, FishingSessionPayoutDto? payout, bool success, bool canceled)
    {
        SessionId = sessionId;
        FishId = fishId;
        Payout = payout;
        Success = success;
        Canceled = canceled;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public Guid FishId { get; set; }

    [Key(2)]
    public FishingSessionPayoutDto? Payout { get; set; }

    [Key(3)]
    public bool Success { get; set; }

    [Key(4)]
    public bool Canceled { get; set; }
}

[MessagePackObject]
public sealed class FishingCanceled : IntersectPacket
{
    public FishingCanceled()
    {
    }

    public FishingCanceled(Guid sessionId, string? reason = null)
    {
        SessionId = sessionId;
        Reason = reason;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public string? Reason { get; set; }
}
