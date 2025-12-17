using Intersect.Network;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class SendClientFish : IntersectPacket
{
    public SendClientFish()
    {
    }
}
