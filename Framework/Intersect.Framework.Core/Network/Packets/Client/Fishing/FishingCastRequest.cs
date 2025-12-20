using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class FishingCastRequest : IntersectPacket
{
    public FishingCastRequest()
    {
    }

    public FishingCastRequest(Guid sessionId)
    {
        SessionId = sessionId;
    }

    [Key(0)]
    public Guid SessionId { get; set; }
}
