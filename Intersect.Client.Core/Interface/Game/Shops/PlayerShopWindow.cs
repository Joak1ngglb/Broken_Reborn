using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;

using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Client.Utilities;
using Intersect.Configuration;
using Intersect.GameObjects;
using Intersect.Network.Packets.Shops;
using Intersect.Client.Items;
using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopWindow : Window
{
    private ScrollControl _inventoryScroll;
    private ScrollControl _listingScroll;
    private TextBox _shopNameInput;
    private TextBoxNumeric _quantityInput;
    private TextBoxNumeric _priceInput;
    private Label _selectedLabel;
    private Label _statusLabel;
    private Button _clearSlotButton;
    private  Button _cancelButton;
    private Button _merchantModeButton;
    private Label _hintLabel;

    private readonly List<PlayerShopInventoryItem> _inventorySlots = new();
    private readonly List<PlayerShopListingSlot> _listingSlots = new();

    private PlayerShopListingSlot? _selectedSlot;
    private bool _inventorySubscribed;
    private bool _uiInitialized;
    private bool _pendingSubmission;

    public PlayerShopWindow(Canvas parent)
        : base(parent, Strings.PlayerShops.CreatorTitle, false, nameof(PlayerShopWindow))
    {
        SetSize(780, 520);
        DisableResizing();
        Closed += WindowOnClosed;
        Disposed += WindowOnClosed;

        InitializeUi();
        UpdateMerchantModeButtonState();
    }

    private void InitializeUi()
    {
        if (_uiInitialized)
        {
            return;
        }

        _uiInitialized = true;

        var nameLabel = new Label(this, "PlayerShopNameLabel")
        {
            Text = Strings.PlayerShops.ShopNameLabel,
        };
        nameLabel.SetBounds(20, 30, 200, 20);

        _shopNameInput = new TextBox(this, "PlayerShopNameInput")
        {
            PlaceholderText = Strings.PlayerShops.ShopNamePlaceholder,
        };
        _shopNameInput.SetBounds(20, 50, 300, 28);
        _shopNameInput.TextChanged += (_, _) => ValidateInputs();

        _hintLabel = new Label(this, "PlayerShopHint")
        {
            Text = Strings.PlayerShops.Hint,
        };
        _hintLabel.SetBounds(20, 86, 320, 40);
        _hintLabel.SetTextColor(Color.Gray, ComponentState.Normal);

        _inventoryScroll = new ScrollControl(this, "PlayerShopInventoryScroll");
        _inventoryScroll.SetBounds(20, 130, 320, 360);
        _inventoryScroll.EnableScroll(false, true);

        _listingScroll = new ScrollControl(this, "PlayerShopListingScroll");
        _listingScroll.SetBounds(360, 30, 180, 300);
        _listingScroll.EnableScroll(false, true);

        _selectedLabel = new Label(this, "PlayerShopSelectedLabel")
        {
            Text = Strings.PlayerShops.SelectSlot,
        };
        _selectedLabel.SetBounds(360, 340, 360, 20);

        _quantityInput = new TextBoxNumeric(this, "PlayerShopQuantityInput")
        {
            Minimum = 1,
        };
        _quantityInput.SetBounds(360, 370, 150, 28);
        _quantityInput.ValueChanged += (_, args) => OnQuantityChanged((int)args.Value);

        _priceInput = new TextBoxNumeric(this, "PlayerShopPriceInput")
        {
            Minimum = 1,
        };
        _priceInput.SetBounds(530, 370, 150, 28);
        _priceInput.ValueChanged += (_, args) => OnPriceChanged((int)args.Value);

        var quantityLabel = new Label(this, "PlayerShopQuantityLabel")
        {
            Text = Strings.PlayerShops.QuantityInput,
        };
        quantityLabel.SetBounds(360, 350, 150, 18);

        var priceLabel = new Label(this, "PlayerShopPriceLabel")
        {
            Text = Strings.PlayerShops.PriceInput,
        };
        priceLabel.SetBounds(530, 350, 150, 18);

        _clearSlotButton = new Button(this, "PlayerShopClearSlot")
        {
            Text = Strings.PlayerShops.ClearSlot,
        };
        _clearSlotButton.SetBounds(360, 410, 150, 32);
        _clearSlotButton.Clicked += (_, _) => ClearSelectedSlot();

        _cancelButton = new Button(this, "PlayerShopCancelButton")
        {
            Text = Strings.PlayerShops.Cancel,
        };
        _cancelButton.SetBounds(360, 450, 150, 32);
        _cancelButton.Clicked += (_, _) => Close();

        _merchantModeButton = new Button(this, "PlayerShopCreateButton")
        {
            Text = Strings.PlayerShops.CreateShop,
        };
        _merchantModeButton.SetBounds(530, 410, 210, 72);
        _merchantModeButton.Clicked += (_, _) => TryCreateShop();

        _statusLabel = new Label(this, "PlayerShopStatus")
        {
            Text = Strings.PlayerShops.StatusReady,
        };
        _statusLabel.SetBounds(360, 500, 380, 20);
        _statusLabel.SetTextColor(Color.ForestGreen, ComponentState.Normal);

        InitInventorySlots();
        InitListingSlots();

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer?.GetResolutionString());
    }

    private void WindowOnClosed(Base? sender, EventArgs args)
    {
        if (_inventorySubscribed && Globals.Me != null)
        {
            Globals.Me.InventoryUpdated -= OnInventoryUpdated;
            _inventorySubscribed = false;
        }
    }

    protected override void EnsureInitialized()
    {
        InitializeUi();
    }

    private void InitInventorySlots()
    {
        _inventorySlots.Clear();
        var max = Math.Max(0, Options.Instance.Player.MaxInventory);
        for (var i = 0; i < max; i++)
        {
            var slot = new PlayerShopInventoryItem(this, _inventoryScroll, i, new ContextMenu(this));
            _inventorySlots.Add(slot);
        }

        PopulateSlotContainer.Populate(_inventoryScroll, _inventorySlots.Cast<SlotItem>().ToList());
    }

    private void InitListingSlots()
    {
        _listingSlots.Clear();
        const int maxListings = 16;
        for (var i = 0; i < maxListings; i++)
        {
            var slot = new PlayerShopListingSlot(this, _listingScroll, i, new ContextMenu(this));
            _listingSlots.Add(slot);
        }

        PopulateSlotContainer.Populate(_listingScroll, _listingSlots.Cast<SlotItem>().ToList());
    }

    public void Update()
    {
        foreach (var slot in _inventorySlots)
        {
            slot.Update();
        }

        foreach (var listing in _listingSlots)
        {
            listing.RefreshInventoryQuantity();
            listing.Update();
        }
    }

    internal void ShowInventoryTooltip(int slotIndex)
    {
        var slot = Globals.Me?.Inventory?[slotIndex];
        if (slot == null || slot.ItemId == Guid.Empty)
        {
            return;
        }

        if (!ItemDescriptor.TryGet(slot.ItemId, out var descriptor))
        {
            return;
        }

        Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, slot.Quantity, slot.ItemProperties);
    }

    internal void HideInventoryTooltip()
    {
        Interface.GameUi.ItemDescriptionWindow?.Hide();
    }

    internal void SelectInventorySlot(int slotIndex)
    {
        var availableListing = _listingSlots.FirstOrDefault(listing => !listing.HasListing);
        if (availableListing != null)
        {
            AssignInventoryToSlot(availableListing, slotIndex);
        }
    }

    internal void SelectListingSlot(PlayerShopListingSlot slot)
    {
        _selectedSlot = slot;
        UpdateSelectedSlotUi();
    }

    private void UpdateSelectedSlotUi()
    {
        if (_selectedSlot is { HasListing: true } listing)
        {
            var descriptor = ItemDescriptor.TryGet(listing.ItemId, out var desc) ? desc : null;
            var name = descriptor?.Name ?? Strings.PlayerShops.EmptySlot;
            _selectedLabel.Text = Strings.PlayerShops.SelectedItemLabel.ToString(name);
            _quantityInput.Enable();
            _priceInput.Enable();
            _clearSlotButton.Enable();
            var max = Math.Max(1, listing.MaxQuantity);
            _quantityInput.SetRange(1, max);
            _quantityInput.Value = Math.Clamp(listing.DesiredQuantity, 1, max);
            _priceInput.SetRange(1, double.NaN);
            _priceInput.Value = Math.Max(1, listing.PricePerUnit);
        }
        else
        {
            _selectedLabel.Text = Strings.PlayerShops.SelectSlot;
            _quantityInput.Disable();
            _priceInput.Disable();
            _clearSlotButton.Disable();
        }
    }

    internal void AssignInventoryToSlot(PlayerShopListingSlot slot, int inventorySlot)
    {
        slot.ConfigureFromInventory(inventorySlot);
        _selectedSlot = slot;
        UpdateSelectedSlotUi();
        DisplayStatus(Strings.PlayerShops.StatusReady, false);
        UpdateMerchantModeButtonState();
    }

    private void OnInventoryUpdated(Player player, int slotIndex)
    {
        foreach (var listing in _listingSlots)
        {
            if (listing.InventorySlot == slotIndex)
            {
                listing.RefreshInventoryQuantity();
            }
        }

        UpdateSelectedSlotUi();
        UpdateMerchantModeButtonState();
    }

    private void OnQuantityChanged(int newValue)
    {
        if (_selectedSlot == null)
        {
            return;
        }

        _selectedSlot.SetDesiredQuantity(newValue);
        UpdateMerchantModeButtonState();
    }

    private void OnPriceChanged(int newValue)
    {
        if (_selectedSlot == null)
        {
            return;
        }

        _selectedSlot.SetPrice(newValue);
        UpdateMerchantModeButtonState();
    }

    private void ValidateInputs()
    {
        DisplayStatus(Strings.PlayerShops.StatusReady, false);
        UpdateMerchantModeButtonState();
    }

    private void DisplayStatus(string message, bool isError)
    {
        _statusLabel.Text = message;
        _statusLabel.SetTextColor(isError ? Color.OrangeRed : Color.ForestGreen, ComponentState.Normal);
    }

    private void ClearSelectedSlot()
    {
        if (_selectedSlot == null)
        {
            return;
        }

        _selectedSlot.Clear();
        _selectedSlot = null;
        UpdateSelectedSlotUi();
        UpdateMerchantModeButtonState();
    }

    internal void ClearSlot(PlayerShopListingSlot slot)
    {
        slot.Clear();
        if (_selectedSlot == slot)
        {
            _selectedSlot = null;
        }

        UpdateSelectedSlotUi();
        UpdateMerchantModeButtonState();
    }

    private void TryCreateShop()
    {
        var name = _shopNameInput.Text?.Trim() ?? string.Empty;

        var activeListings = _listingSlots.Where(slot => slot.HasListing).ToList();
        if (activeListings.Count == 0)
        {
            DisplayStatus(Strings.PlayerShops.ErrorMissingItems, true);
            return;
        }

        var payloads = new List<PlayerShopStockPayload>();
        var usageBySlot = new Dictionary<int, int>();
        foreach (var listing in activeListings)
        {
            if (listing.DesiredQuantity <= 0)
            {
                DisplayStatus(Strings.PlayerShops.ErrorInvalidQuantity, true);
                return;
            }

            if (listing.PricePerUnit <= 0)
            {
                DisplayStatus(Strings.PlayerShops.ErrorInvalidPrice, true);
                return;
            }

            if (!usageBySlot.TryAdd(listing.InventorySlot, listing.DesiredQuantity))
            {
                usageBySlot[listing.InventorySlot] += listing.DesiredQuantity;
            }

            payloads.Add(listing.ToPayload());
        }

        if (Globals.Me?.Inventory == null)
        {
            DisplayStatus(Strings.PlayerShops.ErrorInvalidInventory, true);
            return;
        }

        foreach (var (inventorySlot, required) in usageBySlot)
        {
            if (inventorySlot < 0 || inventorySlot >= Globals.Me.Inventory.Length)
            {
                DisplayStatus(Strings.PlayerShops.ErrorInvalidInventory, true);
                return;
            }

            var slot = Globals.Me.Inventory[inventorySlot];
            if (slot == null || slot.ItemId == Guid.Empty || slot.Quantity < required)
            {
                DisplayStatus(Strings.PlayerShops.ErrorNotEnoughItems.ToString(inventorySlot + 1), true);
                return;
            }
        }

        PacketSender.SendCreatePlayerShop(name, payloads);
        _pendingSubmission = true;
        UpdateMerchantModeButtonState();
        DisplayStatus(Strings.PlayerShops.StatusSubmitting, false);
        Close();
    }

    public void OpenForConfiguration()
    {
        EnsureInventorySubscription();
        _pendingSubmission = false;
        DisplayStatus(Strings.PlayerShops.StatusReady, false);
        RefreshListingState();
        UpdateMerchantModeButtonState();
        Show();
    }

    private void RefreshListingState()
    {
        foreach (var listing in _listingSlots)
        {
            listing.RefreshInventoryQuantity();
        }

        UpdateSelectedSlotUi();
    }

    private void EnsureInventorySubscription()
    {
        if (_inventorySubscribed || Globals.Me == null)
        {
            return;
        }

        Globals.Me.InventoryUpdated += OnInventoryUpdated;
        _inventorySubscribed = true;
    }

    private void UpdateMerchantModeButtonState()
    {
        if (_merchantModeButton == null)
        {
            return;
        }

        if (_pendingSubmission)
        {
            _merchantModeButton.Disable();
            return;
        }

        if (HasValidListingConfiguration())
        {
            _merchantModeButton.Enable();
        }
        else
        {
            _merchantModeButton.Disable();
        }
    }

    private bool HasValidListingConfiguration()
    {
        var listings = _listingSlots.Where(slot => slot.HasListing).ToList();
        if (listings.Count == 0)
        {
            return false;
        }

        return listings.All(listing => listing.DesiredQuantity > 0 && listing.PricePerUnit > 0);
    }

    public new void Show()
    {
        base.Show();
        BringToFront();
    }

    public new void Close()
    {
        base.Close();
    }

    public bool IsVisible()
    {
        return IsVisibleInTree;
    }
}
