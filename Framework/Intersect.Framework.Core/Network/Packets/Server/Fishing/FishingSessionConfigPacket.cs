using System;
using Intersect.Fishing;
using MessagePack;

namespace Intersect.Network.Packets.Server.Fishing;

[MessagePackObject]
public sealed class FishingSessionConfigPacket : IntersectPacket
{
    public FishingSessionConfigPacket()
    {
    }

    public FishingSessionConfigPacket(Guid sessionId, FishingSimConfigDto config, FishingSimStateDto state)
    {
        SessionId = sessionId;
        Config = config;
        State = state;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public FishingSimConfigDto Config { get; set; }

    [Key(2)]
    public FishingSimStateDto State { get; set; }
}
