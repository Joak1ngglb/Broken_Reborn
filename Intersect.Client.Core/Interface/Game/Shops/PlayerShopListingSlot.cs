using System;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Localization;
using Intersect.Client.Items;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Network.Packets.Shops;
using Intersect.Client.Core;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopListingSlot : SlotItem
{
    private readonly PlayerShopWindow _owner;
    private readonly Label _quantityLabel;
    private readonly Label _priceLabel;

    public PlayerShopListingSlot(PlayerShopWindow owner, Base parent, int slotIndex, ContextMenu contextMenu)
        : base(parent, nameof(PlayerShopListingSlot), slotIndex, contextMenu)
    {
        _owner = owner;

        TextureFilename = "inventoryitem.png";
        SetSize(56, 56);

        Icon.SetBounds(8, 8, 40, 40);
        Icon.Clicked += IconOnClicked;

        _quantityLabel = new Label(this, "ListingQuantityLabel")
        {
            Alignment = [Alignments.Bottom, Alignments.Left],
            FontName = "sourcesansproblack",
            FontSize = 8,
            Padding = new Padding(4, 2, 4, 2),
            TextColor = Color.White,
            BackgroundTemplateName = "quantity.png",
        };

        _priceLabel = new Label(this, "ListingPriceLabel")
        {
            Alignment = [Alignments.Bottom, Alignments.Right],
            FontName = "sourcesansproblack",
            FontSize = 8,
            Padding = new Padding(4, 2, 4, 2),
            TextColor = Color.White,
            BackgroundTemplateName = "quantity.png",
        };

        var removeItem = contextMenu.AddItem(Strings.PlayerShops.ClearSlot);
        removeItem.Clicked += (_, _) => _owner.ClearSlot(this);

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    public bool HasListing => ItemId != Guid.Empty && InventorySlot >= 0;

    public Guid ItemId { get; private set; } = Guid.Empty;

    public int InventorySlot { get; private set; } = -1;

    public int DesiredQuantity { get; private set; }

    public int PricePerUnit { get; private set; } = 1;

    public int MaxQuantity { get; private set; }

    public ItemProperties Properties { get; private set; } = new();

    private void IconOnClicked(Base sender, MouseButtonState args)
    {
        if (args.MouseButton == MouseButton.Left)
        {
            _owner.SelectListingSlot(this);
        }
    }

    public override bool DragAndDrop_HandleDrop(Package package, int x, int y)
    {
        if (DragAndDrop.SourceControl?.Parent is InventoryItem inventoryItem)
        {
            _owner.AssignInventoryToSlot(this, inventoryItem.SlotIndex);
            return true;
        }

        return base.DragAndDrop_HandleDrop(package, x, y);
    }

    public override void Update()
    {
        if (!HasListing)
        {
            Reset();
            return;
        }

        if (!ItemDescriptor.TryGet(ItemId, out var descriptor))
        {
            Clear();
            return;
        }

        var texture = GameContentManager.Current.GetTexture(Framework.Content.TextureType.Item, descriptor.Icon);
        if (texture == null)
        {
            Clear();
            return;
        }

        UpdateRarityBorder(descriptor, Icon.IsDragging);

        Icon.Texture = texture;
        Icon.RenderColor = descriptor.Color;
        Icon.IsVisibleInParent = true;
        IsVisibleInParent = true;

        _quantityLabel.IsVisibleInParent = true;
        _priceLabel.IsVisibleInParent = true;
        _quantityLabel.Text = Strings.PlayerShops.SlotQuantity.ToString(DesiredQuantity, Math.Max(1, MaxQuantity));
        _priceLabel.Text = Strings.PlayerShops.SlotPrice.ToString(PricePerUnit);
    }

    public void ConfigureFromInventory(int inventorySlot)
    {
        if (Globals.Me?.Inventory == null)
        {
            return;
        }

        if (inventorySlot < 0 || inventorySlot >= Globals.Me.Inventory.Length)
        {
            return;
        }

        var slot = Globals.Me.Inventory[inventorySlot];
        if (slot == null || slot.ItemId == Guid.Empty)
        {
            Clear();
            return;
        }

        InventorySlot = inventorySlot;
        ItemId = slot.ItemId;
        MaxQuantity = slot.Quantity;
        DesiredQuantity = Math.Max(1, Math.Min(slot.Quantity, DesiredQuantity <= 0 ? slot.Quantity : DesiredQuantity));
        PricePerUnit = Math.Max(1, PricePerUnit);
        Properties = slot.ItemProperties != null ? new ItemProperties(slot.ItemProperties) : new ItemProperties();
        Update();
    }

    public void RefreshInventoryQuantity()
    {
        if (!HasListing || Globals.Me?.Inventory == null)
        {
            return;
        }

        if (InventorySlot < 0 || InventorySlot >= Globals.Me.Inventory.Length)
        {
            Clear();
            return;
        }

        var slot = Globals.Me.Inventory[InventorySlot];
        if (slot == null || slot.ItemId == Guid.Empty)
        {
            Clear();
            return;
        }

        if (slot.ItemId != ItemId)
        {
            Clear();
            return;
        }

        MaxQuantity = slot.Quantity;
        if (DesiredQuantity > MaxQuantity)
        {
            DesiredQuantity = MaxQuantity;
        }
    }

    public void Clear()
    {
        ItemId = Guid.Empty;
        InventorySlot = -1;
        DesiredQuantity = 0;
        PricePerUnit = 1;
        MaxQuantity = 0;
        Properties = new ItemProperties();
        Reset();
    }

    private void Reset()
    {
        Icon.Texture = null;
        Icon.IsVisibleInParent = false;
        _quantityLabel.IsVisibleInParent = false;
        _priceLabel.IsVisibleInParent = false;
        IsVisibleInParent = false;
        ResetRarityBorder();
    }

    public void SetDesiredQuantity(int quantity)
    {
        if (!HasListing)
        {
            return;
        }

        quantity = Math.Clamp(quantity, 1, Math.Max(1, MaxQuantity));
        DesiredQuantity = quantity;
    }

    public void SetPrice(int price)
    {
        if (!HasListing)
        {
            return;
        }

        PricePerUnit = Math.Max(1, price);
    }

    public PlayerShopStockPayload ToPayload()
    {
        return new PlayerShopStockPayload
        {
            ItemId = ItemId,
            Quantity = DesiredQuantity,
            PricePerUnit = PricePerUnit,
            Properties = new ItemProperties(Properties),
            InventorySlot = InventorySlot,
        };
    }
}
