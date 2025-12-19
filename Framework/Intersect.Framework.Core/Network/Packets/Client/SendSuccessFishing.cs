using Intersect.Network;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class SendSuccessFishing : IntersectPacket
{
    public SendSuccessFishing()
    {
    }
}
