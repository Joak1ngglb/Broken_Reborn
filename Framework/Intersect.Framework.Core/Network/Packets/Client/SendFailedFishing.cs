using Intersect.Network;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class SendFailedFishing : IntersectPacket
{
    public SendFailedFishing()
    {
    }
}
