using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public class BuyPlayerShopItemPacket : IntersectPacket
{
    public BuyPlayerShopItemPacket(Guid shopId, Guid shopItemId, int quantity)
    {
        ShopId = shopId;
        ShopItemId = shopItemId;
        Quantity = quantity;
    }

    [Key(0)] public Guid ShopId { get; set; }

    [Key(1)] public Guid ShopItemId { get; set; }

    [Key(2)] public int Quantity { get; set; }
}
