using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class FishingCancelRequest : IntersectPacket
{
    public FishingCancelRequest()
    {
    }

    public FishingCancelRequest(Guid sessionId, string reason)
    {
        SessionId = sessionId;
        Reason = reason;
    }

    [Key(0)]
    public Guid SessionId { get; set; }

    [Key(1)]
    public string Reason { get; set; } = string.Empty;
}
