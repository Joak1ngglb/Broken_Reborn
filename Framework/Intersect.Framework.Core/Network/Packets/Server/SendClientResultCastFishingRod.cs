using Intersect.Network;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class SendClientResultCastFishingRod : IntersectPacket
{
    public SendClientResultCastFishingRod()
    {
    }
}
