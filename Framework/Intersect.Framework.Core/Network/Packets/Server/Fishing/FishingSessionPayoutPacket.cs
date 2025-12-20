using System;
using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Server.Fishing;

[MessagePackObject]
public sealed class FishingSessionPayoutPacket : IntersectPacket
{
    public FishingSessionPayoutPacket()
    {
    }

    public FishingSessionPayoutPacket(Guid sessionId, FishingSessionPayoutDto payout, bool success)
    {
        SessionId = sessionId;
        Payout = payout;
        Success = success;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public FishingSessionPayoutDto Payout { get; set; }

    [Key(2)]
    public bool Success { get; set; }
}

[MessagePackObject]
public sealed class FishingSessionPayoutDto
{
    [Key(0)]
    public long Experience { get; set; }

    [Key(1)]
    public long Currency { get; set; }

    [Key(2)]
    public Dictionary<Guid, int> Items { get; set; } = new();
}
