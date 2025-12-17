using Intersect.Network;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class SendClientResultCastFishingRod : IntersectPacket
{
    public SendClientResultCastFishingRod()
    {
    }

    public SendClientResultCastFishingRod(bool success)
    {
        Success = success;
    }

    [Key(0)]
    public bool Success { get; set; }
}
