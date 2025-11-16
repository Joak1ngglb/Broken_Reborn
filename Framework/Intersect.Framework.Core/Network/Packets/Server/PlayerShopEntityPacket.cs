using System;
using System.Collections.Generic;
using MessagePack;
using Intersect.Enums;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class PlayerShopEntityPacket : EntityPacket
{
    public PlayerShopEntityPacket()
    {
    }

    [Key(24)]
    public Guid ShopId { get; set; }

    [Key(25)]
    public Gender Gender { get; set; }

    [Key(26)]
    public Dictionary<int, List<Guid>> Equipment { get; set; } = new();
}
