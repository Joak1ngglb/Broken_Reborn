using System;
using Intersect.Enums;
using Intersect.Framework.Core.Entities;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public sealed class PlayerShopEntity : Entity
{
    public PlayerShopEntity(Guid id, PlayerShopEntityPacket packet) : base(id, packet, EntityType.PlayerShop)
    {
        EnsureSprite();
    }

    public Guid ShopId { get; private set; }

    public override void Load(EntityPacket? packet)
    {
        base.Load(packet);

        if (packet is not PlayerShopEntityPacket shopEntityPacket)
        {
            return;
        }

        ShopId = shopEntityPacket.ShopId;

        EnsureSprite();
    }

    public override void Draw()
    {
        EnsureSprite();
        base.Draw();
    }

    private void EnsureSprite()
    {
        if (string.IsNullOrWhiteSpace(Sprite))
        {
            Sprite = PlayerShopEntityConstants.DefaultSprite;
        }

        if (Texture == null && !string.IsNullOrWhiteSpace(Sprite))
        {
            LoadTextures(Sprite);
        }
    }
}
