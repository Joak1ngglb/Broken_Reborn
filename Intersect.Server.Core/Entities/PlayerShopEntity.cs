using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly Dictionary<int, List<Guid>> _equipment;

    public PlayerShopEntity(
        PlayerShopManager.PlayerShopRuntime runtime,
        Guid mapInstanceId,
        string? sprite = null,
        string? face = null,
        Color? nameColor = null,
        Label? headerLabel = null,
        Label? footerLabel = null,
        string? decoration = null
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
            ? runtime.Sprite
            : sprite;
        Face = face ?? runtime.Face ?? string.Empty;
        Color = runtime.Color;
        Gender = runtime.Gender;
        NameColor = nameColor ?? Color.White;
        HeaderLabel = headerLabel ?? new Label(runtime.Title, Color.White);
        FooterLabel = footerLabel ?? new Label(runtime.OwnerName, Color.White);
        Decoration = string.IsNullOrWhiteSpace(decoration)
            ? runtime.Decoration
            : decoration;

        _equipment = runtime.Equipment?.ToDictionary(
                pair => pair.Key,
                pair => pair.Value != null ? new List<Guid>(pair.Value) : new List<Guid>()
            )
            ?? new Dictionary<int, List<Guid>>();

        Passable = false;
        HideName = false;
    }

    public Guid ShopId { get; }

    public Guid OwnerId { get; }

    public string OwnerName { get; }

    public Gender Gender { get; }

    public string Decoration { get; }

    public IReadOnlyDictionary<int, List<Guid>> Equipment => _equipment;

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
        playerShopEntityPacket.Gender = Gender;
        playerShopEntityPacket.Equipment = _equipment.ToDictionary(
            pair => pair.Key,
            pair => pair.Value != null ? new List<Guid>(pair.Value) : new List<Guid>()
        );
        playerShopEntityPacket.Decoration = Decoration;

        return playerShopEntityPacket;
    }
}
