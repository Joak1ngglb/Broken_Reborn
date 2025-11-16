using System;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game;
using Intersect.Client.Localization;
using Intersect.Client.Items;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Core;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopInventoryItem : SlotItem
{
    private readonly PlayerShopWindow _owner;
    private readonly Label _quantityLabel;

    public PlayerShopInventoryItem(PlayerShopWindow owner, Base parent, int slotIndex, ContextMenu contextMenu)
        : base(parent, nameof(PlayerShopInventoryItem), slotIndex, contextMenu)
    {
        _owner = owner;

        TextureFilename = "inventoryitem.png";
        SetSize(36, 36);

        Icon.SetBounds(4, 4, 32, 32);
        Icon.HoverEnter += IconOnHoverEnter;
        Icon.HoverLeave += IconOnHoverLeave;
        Icon.Clicked += IconOnClicked;

        _quantityLabel = new Label(this, "InventoryQuantityLabel")
        {
            Alignment = [Alignments.Bottom, Alignments.Right],
            BackgroundTemplateName = "quantity.png",
            FontName = "sourcesansproblack",
            FontSize = 8,
            Padding = new Padding(2),
        };

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    private void IconOnHoverEnter(Base sender, EventArgs args)
    {
        _owner.ShowInventoryTooltip(SlotIndex);
    }

    private void IconOnHoverLeave(Base sender, EventArgs args)
    {
        _owner.HideInventoryTooltip();
    }

    private void IconOnClicked(Base sender, MouseButtonState args)
    {
        if (args.MouseButton == MouseButton.Left)
        {
            _owner.SelectInventorySlot(SlotIndex);
        }
    }

    public override void Update()
    {
        if (Globals.Me?.Inventory == null)
        {
            Reset();
            return;
        }

        var slot = Globals.Me.Inventory[SlotIndex];
        if (slot == null || slot.ItemId == Guid.Empty)
        {
            Reset();
            return;
        }

        if (!ItemDescriptor.TryGet(slot.ItemId, out var descriptor))
        {
            Reset();
            return;
        }

        var texture = GameContentManager.Current.GetTexture(Framework.Content.TextureType.Item, descriptor.Icon);
        if (texture == null)
        {
            Reset();
            return;
        }

        Icon.Texture = texture;
        Icon.RenderColor = descriptor.Color;
        Icon.IsVisibleInParent = true;
        IsVisibleInParent = true;

        var showQuantity = descriptor.Stackable && slot.Quantity > 1;
        _quantityLabel.IsVisibleInParent = showQuantity;
        if (showQuantity)
        {
            _quantityLabel.Text = Strings.FormatQuantityAbbreviated(slot.Quantity);
        }
    }

    private void Reset()
    {
        Icon.Texture = null;
        Icon.IsVisibleInParent = false;
        _quantityLabel.IsVisibleInParent = false;
        IsVisibleInParent = false;
    }
}
