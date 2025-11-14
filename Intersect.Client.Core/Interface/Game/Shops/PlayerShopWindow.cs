using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.Component;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Client.Utilities;
using Intersect.Configuration;
using Intersect.GameObjects;
using Intersect.Network.Packets.Shops;
using Intersect.Client.Items;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopWindow
{
    private readonly WindowControl _window;
    private readonly ScrollControl _inventoryScroll;
    private readonly ScrollControl _listingScroll;
    private readonly TextBox _shopNameInput;
    private readonly TextBoxNumeric _quantityInput;
    private readonly TextBoxNumeric _priceInput;
    private readonly Label _selectedLabel;
    private readonly Label _statusLabel;
    private readonly Button _clearSlotButton;
    private readonly Button _createButton;
    private readonly Label _hintLabel;

    private readonly List<PlayerShopInventoryItem> _inventorySlots = new();
    private readonly List<PlayerShopListingSlot> _listingSlots = new();

    private PlayerShopListingSlot? _selectedSlot;
    private bool _inventorySubscribed;

    public event Action<PlayerShopWindow>? Closed;

    public PlayerShopWindow(Canvas parent)
    {
        _window = new WindowControl(parent, Strings.PlayerShops.CreatorTitle, false, nameof(PlayerShopWindow));
        _window.SetSize(780, 520);
        _window.DisableResizing();
        _window.Closed += WindowOnClosed;
        _window.Disposed += WindowOnClosed;

        var nameLabel = new Label(_window, "PlayerShopNameLabel")
        {
            Text = Strings.PlayerShops.ShopNameLabel,
        };
        nameLabel.SetBounds(20, 30, 200, 20);

        _shopNameInput = new TextBox(_window, "PlayerShopNameInput")
        {
            PlaceholderText = Strings.PlayerShops.ShopNamePlaceholder,
        };
        _shopNameInput.SetBounds(20, 50, 300, 28);
        _shopNameInput.TextChanged += (_, _) => ValidateInputs();

        _hintLabel = new Label(_window, "PlayerShopHint")
        {
            Text = Strings.PlayerShops.Hint,
        };
        _hintLabel.SetBounds(20, 86, 320, 40);
        _hintLabel.SetTextColor(Color.Gray, ComponentState.Normal);

        _inventoryScroll = new ScrollControl(_window, "PlayerShopInventoryScroll");
        _inventoryScroll.SetBounds(20, 130, 320, 360);
        _inventoryScroll.EnableScroll(false, true);

        _listingScroll = new ScrollControl(_window, "PlayerShopListingScroll");
        _listingScroll.SetBounds(360, 30, 180, 300);
        _listingScroll.EnableScroll(false, true);

        _selectedLabel = new Label(_window, "PlayerShopSelectedLabel")
        {
            Text = Strings.PlayerShops.SelectSlot,
        };
        _selectedLabel.SetBounds(360, 340, 360, 20);

        _quantityInput = new TextBoxNumeric(_window, "PlayerShopQuantityInput")
        {
            Minimum = 1,
        };
        _quantityInput.SetBounds(360, 370, 150, 28);
        _quantityInput.ValueChanged += (_, args) => OnQuantityChanged((int)args.Value);

        _priceInput = new TextBoxNumeric(_window, "PlayerShopPriceInput")
        {
            Minimum = 1,
        };
        _priceInput.SetBounds(530, 370, 150, 28);
        _priceInput.ValueChanged += (_, args) => OnPriceChanged((int)args.Value);

        var quantityLabel = new Label(_window, "PlayerShopQuantityLabel")
        {
            Text = Strings.PlayerShops.QuantityInput,
        };
        quantityLabel.SetBounds(360, 350, 150, 18);

        var priceLabel = new Label(_window, "PlayerShopPriceLabel")
        {
            Text = Strings.PlayerShops.PriceInput,
        };
        priceLabel.SetBounds(530, 350, 150, 18);

        _clearSlotButton = new Button(_window, "PlayerShopClearSlot")
        {
            Text = Strings.PlayerShops.ClearSlot,
        };
        _clearSlotButton.SetBounds(360, 410, 150, 32);
        _clearSlotButton.Clicked += (_, _) => ClearSelectedSlot();

        _createButton = new Button(_window, "PlayerShopCreateButton")
        {
            Text = Strings.PlayerShops.CreateShop,
        };
        _createButton.SetBounds(530, 410, 210, 40);
        _createButton.Clicked += (_, _) => TryCreateShop();

        _statusLabel = new Label(_window, "PlayerShopStatus")
        {
            Text = Strings.PlayerShops.StatusReady,
        };
        _statusLabel.SetBounds(360, 460, 380, 20);
        _statusLabel.SetTextColor(Color.LightGreen, ComponentState.Normal);

        InitInventorySlots();
        InitListingSlots();

        Globals.Me.InventoryUpdated += OnInventoryUpdated;
        _inventorySubscribed = true;
    }

    private void WindowOnClosed(Base? sender, EventArgs args)
    {
        if (_inventorySubscribed && Globals.Me != null)
        {
            Globals.Me.InventoryUpdated -= OnInventoryUpdated;
            _inventorySubscribed = false;
        }

        Closed?.Invoke(this);
    }

    private void InitInventorySlots()
    {
        _inventorySlots.Clear();
        var max = Math.Max(0, Options.Instance.Player.MaxInventory);
        for (var i = 0; i < max; i++)
        {
            var slot = new PlayerShopInventoryItem(this, _inventoryScroll, i, new ContextMenu(_window));
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
            var slot = new PlayerShopListingSlot(this, _listingScroll, i, new ContextMenu(_window));
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

        Interface.Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, slot.Quantity, slot.ItemProperties);
    }

    internal void HideInventoryTooltip()
    {
        Interface.Interface.GameUi.ItemDescriptionWindow?.Hide();
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
    }

    private void OnQuantityChanged(int newValue)
    {
        if (_selectedSlot == null)
        {
            return;
        }

        _selectedSlot.SetDesiredQuantity(newValue);
    }

    private void OnPriceChanged(int newValue)
    {
        if (_selectedSlot == null)
        {
            return;
        }

        _selectedSlot.SetPrice(newValue);
    }

    private void ValidateInputs()
    {
        var valid = !string.IsNullOrWhiteSpace(_shopNameInput.Text);
        if (!valid)
        {
            DisplayStatus(Strings.PlayerShops.ErrorMissingName, true);
        }
        else
        {
            DisplayStatus(Strings.PlayerShops.StatusReady, false);
        }
    }

    private void DisplayStatus(string message, bool isError)
    {
        _statusLabel.Text = message;
        _statusLabel.SetTextColor(isError ? Color.OrangeRed : Color.LightGreen, ComponentState.Normal);
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
    }

    internal void ClearSlot(PlayerShopListingSlot slot)
    {
        slot.Clear();
        if (_selectedSlot == slot)
        {
            _selectedSlot = null;
        }

        UpdateSelectedSlotUi();
    }

    private void TryCreateShop()
    {
        var name = _shopNameInput.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            DisplayStatus(Strings.PlayerShops.ErrorMissingName, true);
            return;
        }

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
        DisplayStatus(Strings.PlayerShops.StatusSubmitting, false);
        Close();
    }

    public void Show()
    {
        _window.Show();
        _window.BringToFront();
    }

    public void Close()
    {
        _window.Close();
    }

    public bool IsVisible()
    {
        return _window?.IsVisibleInTree ?? false;
    }
}
