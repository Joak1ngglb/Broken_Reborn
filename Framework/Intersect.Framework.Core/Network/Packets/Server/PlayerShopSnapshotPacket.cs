using Intersect.Network.Packets.Shops;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public class PlayerShopSnapshotPacket : IntersectPacket
{
    public PlayerShopSnapshotPacket(ShopSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    [Key(0)] public ShopSnapshot Snapshot { get; set; } = new();
}
