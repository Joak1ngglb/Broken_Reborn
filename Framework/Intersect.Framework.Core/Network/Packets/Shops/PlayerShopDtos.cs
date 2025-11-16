using System;
using System.Collections.Generic;
using Intersect.Framework.Core.GameObjects.Items;
using MessagePack;

namespace Intersect.Network.Packets.Shops;

[MessagePackObject]
public class PlayerShopItemSnapshot
{
    [Key(0)] public Guid ShopItemId { get; set; }

    [Key(1)] public Guid ItemId { get; set; }

    [Key(2)] public int Quantity { get; set; }

    [Key(3)] public int PricePerUnit { get; set; }

    [Key(4)] public ItemProperties Properties { get; set; } = new();
}

[MessagePackObject]
public class ShopSnapshot
{
    [Key(0)] public Guid ShopId { get; set; }

    [Key(1)] public string Name { get; set; } = string.Empty;

    [Key(2)] public string OwnerName { get; set; } = string.Empty;

    [Key(3)] public List<PlayerShopItemSnapshot> Items { get; set; } = new();
}

[MessagePackObject]
public class PlayerShopStockPayload
{
    [Key(0)] public Guid ItemId { get; set; }

    [Key(1)] public int Quantity { get; set; }

    [Key(2)] public int PricePerUnit { get; set; }

    [Key(3)] public ItemProperties Properties { get; set; } = new();

    [Key(4)] public int InventorySlot { get; set; }
}
