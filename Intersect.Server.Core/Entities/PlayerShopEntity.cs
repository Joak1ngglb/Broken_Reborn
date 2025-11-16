using System;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Network.Packets.Server;
using Intersect.Server.Database.PlayerData.Shops;
using Intersect.Server.Framework.Entities;
using Intersect.Server.Framework.Items;

namespace Intersect.Server.Entities;

public sealed class PlayerShopEntity : Entity
{
    public PlayerShopEntity(
        PlayerShopManager.PlayerShopRuntime runtime,
        Guid mapInstanceId,
        string? sprite = null,
        string? face = null,
        Color? nameColor = null,
        Label? headerLabel = null,
        Label? footerLabel = null
    ) : base(runtime?.ShopId ?? throw new ArgumentNullException(nameof(runtime)), mapInstanceId)
    {
        MapId = runtime.MapId;
        X = runtime.X;
        Y = runtime.Y;
        Z = runtime.Z;
        Dir = Direction.Down;
        Name = runtime.Title;

        ShopId = runtime.ShopId;
        OwnerId = runtime.OwnerId;
        OwnerName = runtime.OwnerName;

        Sprite = string.IsNullOrWhiteSpace(sprite)
            ? PlayerShopEntityConstants.DefaultSprite
            : sprite;
        Face = face ?? string.Empty;
        NameColor = nameColor ?? Color.White;
        HeaderLabel = headerLabel ?? new Label(runtime.Title, Color.White);
        FooterLabel = footerLabel ?? new Label(runtime.OwnerName, Color.White);

        Passable = false;
        HideName = false;
    }

    public Guid ShopId { get; }

    public Guid OwnerId { get; }

    public string OwnerName { get; }

    public override EntityType GetEntityType() => EntityType.PlayerShop;

    protected override EntityItemSource? AsItemSource() => null;

    public override EntityPacket EntityPacket(EntityPacket? packet = null, Player? forPlayer = null)
    {
        packet ??= new PlayerShopEntityPacket();
        packet = base.EntityPacket(packet, forPlayer);

        if (packet is not PlayerShopEntityPacket playerShopEntityPacket)
        {
            throw new InvalidOperationException(
                $"Invalid packet type '{packet.GetType().FullName}', expected '{typeof(PlayerShopEntityPacket).FullName}'"
            );
        }

        playerShopEntityPacket.ShopId = ShopId;

        return playerShopEntityPacket;
    }
}
