using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public sealed class PlayerShopEntity : Entity
{
    private IGameTexture? _decorationTexture;
    private string _decorationName = PlayerShopEntityConstants.DefaultDecoration;
    private string _loadedDecoration = string.Empty;

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
        Gender = shopEntityPacket.Gender;
        Equipment = NormalizeEquipment(shopEntityPacket.Equipment);
        _decorationName = string.IsNullOrWhiteSpace(shopEntityPacket.Decoration)
            ? PlayerShopEntityConstants.DefaultDecoration
            : shopEntityPacket.Decoration;

        EnsureSprite();
    }

    public override void Draw()
    {
        EnsureSprite();
        base.Draw();
        DrawDecoration();
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

    private static Dictionary<int, List<Guid>> NormalizeEquipment(Dictionary<int, List<Guid>>? equipment)
    {
        var slotCount = Options.Instance.Equipment.Slots.Count;
        var normalized = new Dictionary<int, List<Guid>>(slotCount);

        if (equipment != null)
        {
            foreach (var pair in equipment)
            {
                normalized[pair.Key] = pair.Value != null ? new List<Guid>(pair.Value) : new List<Guid>();
            }
        }

        for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            if (!normalized.ContainsKey(slotIndex))
            {
                normalized[slotIndex] = new List<Guid>();
            }
        }

        return normalized;
    }

    private IGameTexture? EnsureDecorationTexture()
    {
        var decorationName = string.IsNullOrWhiteSpace(_decorationName)
            ? PlayerShopEntityConstants.DefaultDecoration
            : _decorationName;

        if (!string.Equals(decorationName, _loadedDecoration, StringComparison.OrdinalIgnoreCase))
        {
            _decorationTexture = Globals.ContentManager.GetTexture(TextureType.Entity, decorationName);
            _loadedDecoration = decorationName;

            if (_decorationTexture == null)
            {
                _decorationTexture = Globals.ContentManager.GetTexture(
                    TextureType.Entity,
                    PlayerShopEntityConstants.DefaultDecoration
                );
                _loadedDecoration = PlayerShopEntityConstants.DefaultDecoration;
            }
        }

        _decorationTexture ??= Globals.ContentManager.GetTexture(TextureType.Entity, PlayerShopEntityConstants.DefaultDecoration);

        return _decorationTexture;
    }

    private void DrawDecoration()
    {
        var decoration = EnsureDecorationTexture();
        if (decoration == null)
        {
            return;
        }

        var frameWidth = decoration.Width / Options.Instance.Sprites.NormalFrames;
        var frameHeight = decoration.Height / Options.Instance.Sprites.Directions;
        var srcRectangle = new FloatRect(0, (int)Direction.Down * frameHeight, frameWidth, frameHeight);
        var destRectangle = new FloatRect(
            (float)Math.Ceiling(WorldPos.X + WorldPos.Width / 2f - frameWidth / 2f),
            (float)Math.Ceiling(WorldPos.Y + WorldPos.Height - frameHeight * 0.3f),
            frameWidth,
            frameHeight
        );

        Graphics.DrawGameTexture(decoration, srcRectangle, destRectangle, Color.White);
    }
}
