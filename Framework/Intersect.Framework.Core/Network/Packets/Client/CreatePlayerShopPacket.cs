using System.Collections.Generic;
using Intersect.Network.Packets.Shops;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public class CreatePlayerShopPacket : IntersectPacket
{
    public CreatePlayerShopPacket(string name, List<PlayerShopStockPayload> stock, string? decoration)
    {
        Name = name;
        Stock = stock ?? new List<PlayerShopStockPayload>();
        Decoration = decoration ?? string.Empty;
    }

    [Key(0)] public string Name { get; set; } = string.Empty;

    [Key(1)] public List<PlayerShopStockPayload> Stock { get; set; } = new();

    [Key(2)] public string Decoration { get; set; } = string.Empty;
}
