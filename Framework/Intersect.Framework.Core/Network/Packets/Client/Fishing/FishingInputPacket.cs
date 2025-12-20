using System;
using Intersect.Fishing;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class FishingInputPacket : IntersectPacket
{
    public FishingInputPacket()
    {
    }

    public FishingInputPacket(Guid sessionId, int tickMs, FishingInputFlags flags)
    {
        SessionId = sessionId;
        TickMs = tickMs;
        Flags = flags;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public int TickMs { get; set; }

    [Key(2)]
    public FishingInputFlags Flags { get; set; }
}
