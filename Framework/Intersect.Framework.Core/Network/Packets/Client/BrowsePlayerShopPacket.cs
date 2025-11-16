using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public class BrowsePlayerShopPacket : IntersectPacket
{
    public BrowsePlayerShopPacket(Guid shopId)
    {
        ShopId = shopId;
    }

    [Key(0)] public Guid ShopId { get; set; }
}
