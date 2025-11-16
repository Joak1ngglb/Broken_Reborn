using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class PlayerShopEntityPacket : EntityPacket
{
    public PlayerShopEntityPacket()
    {
    }

    [Key(24)]
    public Guid ShopId { get; set; }
}
