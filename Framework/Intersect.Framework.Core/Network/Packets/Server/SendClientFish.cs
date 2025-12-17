using System;
using Intersect.Network;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class SendClientFish : IntersectPacket
{
    public SendClientFish()
    {
    }

    public SendClientFish(Guid fishId)
    {
        FishId = fishId;
    }

    [Key(0)]
    public Guid FishId { get; set; }
}
