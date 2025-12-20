using System;
using Intersect.Fishing;
using MessagePack;

namespace Intersect.Network.Packets.Server.Fishing;

[MessagePackObject]
public sealed class FishingSessionInputPacket : IntersectPacket
{
    public FishingSessionInputPacket()
    {
    }

    public FishingSessionInputPacket(Guid sessionId, uint tickMs, FishingInputFlags flags)
    {
        SessionId = sessionId;
        TickMs = tickMs;
        Flags = flags;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public uint TickMs { get; set; }

    [Key(2)]
    public FishingInputFlags Flags { get; set; }
}
