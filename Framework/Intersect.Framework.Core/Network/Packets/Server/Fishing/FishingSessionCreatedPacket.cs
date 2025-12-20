using System;
using MessagePack;

namespace Intersect.Network.Packets.Server.Fishing;

[MessagePackObject]
public sealed class FishingSessionCreatedPacket : IntersectPacket
{
    public FishingSessionCreatedPacket()
    {
    }

    public FishingSessionCreatedPacket(Guid sessionId, uint seed)
    {
        SessionId = sessionId;
        Seed = seed;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public uint Seed { get; set; }
}
