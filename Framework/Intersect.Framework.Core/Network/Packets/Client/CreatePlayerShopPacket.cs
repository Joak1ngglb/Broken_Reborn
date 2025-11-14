using System.Collections.Generic;
using Intersect.Network.Packets.Shops;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public class CreatePlayerShopPacket : IntersectPacket
{
    public CreatePlayerShopPacket(string name, List<PlayerShopStockPayload> stock)
    {
        Name = name;
        Stock = stock ?? new List<PlayerShopStockPayload>();
    }

    [Key(0)] public string Name { get; set; } = string.Empty;

    [Key(1)] public List<PlayerShopStockPayload> Stock { get; set; } = new();
}
