using System;
using Intersect.Fishing;
using MessagePack;

namespace Intersect.Network.Packets.Server.Fishing;

[MessagePackObject]
public sealed class FishingSessionStatePacket : IntersectPacket
{
    public FishingSessionStatePacket()
    {
    }

    public FishingSessionStatePacket(Guid sessionId, FishingSimStateDto state)
    {
        SessionId = sessionId;
        State = state;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public FishingSimStateDto State { get; set; }
}
