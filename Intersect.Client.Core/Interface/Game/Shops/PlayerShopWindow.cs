using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
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
using Intersect.Framework.Core.Entities;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopWindow : Window
{
    private ScrollControl _inventoryScroll;
    private ScrollControl _listingScroll;
    private TextBox _shopNameInput;
    private TextBoxNumeric _quantityInput;
    private TextBoxNumeric _priceInput;
    private Label _selectedLabel;
    private Label _selectedRarityLabel;
    private Label _selectedDescriptionLabel;
    private Label _selectedPricePreviewLabel;
    private Label _statusLabel;
    private Button _clearSlotButton;
    private Button _cancelButton;
    private Button _merchantModeButton;
    private Label _hintLabel;
    private LabeledComboBox _decorationSelector;

    private readonly List<PlayerShopInventoryItem> _inventorySlots = new();
    private readonly List<PlayerShopListingSlot> _listingSlots = new();

    private string _selectedDecoration = PlayerShopEntityConstants.DefaultDecoration;

    private PlayerShopListingSlot? _selectedSlot;
    private bool _inventorySubscribed;
    private bool _uiInitialized;
    private bool _pendingSubmission;

    public PlayerShopWindow(Canvas parent)
        : base(parent, Strings.PlayerShops.CreatorTitle, false, nameof(PlayerShopWindow))
    {
        SetSize(980, 620);
        IsResizable = false;

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

        var listingZone = new ImagePanel(this, "PlayerShopListingZone")
        {
            Texture = Graphics.Renderer?.WhitePixel,
            RenderColor = new Color(24, 28, 36, 230),
        };
        listingZone.SetBounds(16, 16, 610, 520);

        var detailZone = new ImagePanel(this, "PlayerShopDetailZone")
        {
            Texture = Graphics.Renderer?.WhitePixel,
            RenderColor = new Color(18, 22, 30, 230),
        };
        detailZone.SetBounds(642, 16, 322, 520);

        var footerZone = new ImagePanel(this, "PlayerShopFooterZone")
        {
            Texture = Graphics.Renderer?.WhitePixel,
            RenderColor = new Color(12, 16, 22, 245),
        };
        footerZone.SetBounds(16, 548, 948, 56);

        // Nombre de la tienda
        var nameLabel = new Label(this, "PlayerShopNameLabel")
        {
            Text = Strings.PlayerShops.ShopNameLabel,
        };
        nameLabel.SetBounds(28, 28, 240, 20);

        _shopNameInput = new TextBox(this, "PlayerShopNameInput")
        {
            PlaceholderText = Strings.PlayerShops.ShopNamePlaceholder,
        };
        _shopNameInput.SetBounds(28, 52, 360, 28);
        _shopNameInput.TextChanged += (_, _) => ValidateInputs();

        // Hint compacto debajo del nombre
        _hintLabel = new Label(this, "PlayerShopHint")
        {
            Text = Strings.PlayerShops.Hint,
        };
        _hintLabel.SetBounds(28, 84, 560, 20);
        _hintLabel.SetTextColor(Color.Gray, ComponentState.Normal);

        _decorationSelector = new LabeledComboBox(this, "PlayerShopDecorationSelector")
        {
            AutoSizeToContents = false,
            Label = Strings.PlayerShops.DecorationLabel,
        };
        _decorationSelector.SetBounds(406, 32, 200, 64);
        _decorationSelector.ItemSelected += (_, args) => OnDecorationSelected(args.SelectedUserData as string);

        // Grid de inventario/listados (izquierda-centro)
        _inventoryScroll = new ScrollControl(this, "PlayerShopInventoryScroll");
        _inventoryScroll.SetBounds(28, 132, 280, 392);
        _inventoryScroll.EnableScroll(false, true);

        _listingScroll = new ScrollControl(this, "PlayerShopListingScroll");
        _listingScroll.SetBounds(324, 132, 286, 392);
        _listingScroll.EnableScroll(false, true);

        var inventoryTitle = new Label(this, "PlayerShopInventoryTitle")
        {
            Text = Strings.Inventory.Title,
        };
        inventoryTitle.SetBounds(28, 112, 200, 18);

        var listingTitle = new Label(this, "PlayerShopListingTitle")
        {
            Text = Strings.PlayerShops.CreateShop,
        };
        listingTitle.SetBounds(324, 112, 200, 18);

        // Panel de detalle seleccionado (derecha)
        _selectedLabel = new Label(this, "PlayerShopSelectedLabel")
        {
            Text = Strings.PlayerShops.SelectSlot,
            FontName = "sourcesansproblack",
        };
        _selectedLabel.SetBounds(658, 34, 292, 24);

        _selectedRarityLabel = new Label(this, "PlayerShopSelectedRarity")
        {
            Text = string.Empty,
            BackgroundTemplateName = "quantity.png",
            Alignment = [Alignments.Center],
            FontName = "sourcesansproblack",
            FontSize = 9,
            Padding = new Padding(4, 2, 4, 2),
        };
        _selectedRarityLabel.SetBounds(658, 64, 140, 20);

        _selectedDescriptionLabel = new Label(this, "PlayerShopSelectedDescription")
        {
            Text = Strings.PlayerShops.SelectSlot,
            WrappingBehavior = WrappingBehavior.Wrapped,
        };
        _selectedDescriptionLabel.SetBounds(658, 92, 292, 128);

        _selectedPricePreviewLabel = new Label(this, "PlayerShopPricePreview")
        {
            Text = Strings.PlayerShops.BrowserPriceEach.ToString(0),
            BackgroundTemplateName = "quantity.png",
            FontName = "sourcesansproblack",
            FontSize = 10,
            Padding = new Padding(6, 3, 6, 3),
            TextColor = new Color(255, 235, 120),
        };
        _selectedPricePreviewLabel.SetBounds(658, 228, 292, 28);

        var quantityLabel = new Label(this, "PlayerShopQuantityLabel")
        {
            Text = Strings.PlayerShops.QuantityInput,
        };
        quantityLabel.SetBounds(658, 272, 140, 18);

        var priceLabel = new Label(this, "PlayerShopPriceLabel")
        {
            Text = Strings.PlayerShops.PriceInput,
        };
        priceLabel.SetBounds(658, 334, 140, 18);

        _quantityInput = new TextBoxNumeric(this, "PlayerShopQuantityInput")
        {
            Minimum = 1,
        };
        _quantityInput.SetBounds(658, 294, 292, 34);
        _quantityInput.ValueChanged += (_, args) => OnQuantityChanged((int)args.Value);

        _priceInput = new TextBoxNumeric(this, "PlayerShopPriceInput")
        {
            Minimum = 1,
        };
        _priceInput.SetBounds(658, 356, 292, 34);
        _priceInput.ValueChanged += (_, args) => OnPriceChanged((int)args.Value);

        _clearSlotButton = new Button(this, "PlayerShopClearSlot")
        {
            Text = Strings.PlayerShops.ClearSlot,
        };
        _clearSlotButton.SetBounds(658, 404, 292, 36);
        _clearSlotButton.Clicked += (_, _) => ClearSelectedSlot();

        _cancelButton = new Button(this, "PlayerShopCancelButton")
        {
            Text = Strings.PlayerShops.Cancel,
        };
        _cancelButton.SetBounds(28, 558, 180, 36);
        _cancelButton.Clicked += (_, _) => Close();

        _merchantModeButton = new Button(this, "PlayerShopCreateButton")
        {
            Text = Strings.PlayerShops.CreateShop,
        };
        _merchantModeButton.SetBounds(220, 558, 220, 36);
        _merchantModeButton.Clicked += (_, _) => TryCreateShop();

        // Estado en barra inferior
        _statusLabel = new Label(this, "PlayerShopStatus")
        {
            Text = Strings.PlayerShops.StatusReady,
        };
        _statusLabel.SetBounds(460, 564, 496, 24);
        _statusLabel.SetTextColor(Color.ForestGreen, ComponentState.Normal);

        // Legacy controls replaced by detail panel.

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer?.GetResolutionString());
        PopulateDecorationSelector();
        InitInventorySlots();
        InitListingSlots();
        UpdateSelectedSlotUi();
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

            // Asegura tamaño para evitar divisiones por cero en grids
            if (slot.Width <= 0 || slot.Height <= 0)
            {
                slot.SetSize(36, 36);
            }

            _inventorySlots.Add(slot);
        }

        if (_inventorySlots.Count > 0)
        {
            PopulateSlotContainer.Populate(_inventoryScroll, _inventorySlots.Cast<SlotItem>().ToList());
        }
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
            _selectedDescriptionLabel.Text = descriptor?.Description ?? Strings.PlayerShops.Hint;

            if (descriptor != null && descriptor.Rarity > 0)
            {
                var rarityLabel = Strings.GetLocalizedItemRarityName(descriptor.Rarity);
                _selectedRarityLabel.Text = rarityLabel;
                _selectedRarityLabel.IsVisibleInParent = true;

                if (CustomColors.Items.Rarities.TryGetValue(descriptor.Rarity, out var rarityColor))
                {
                    _selectedRarityLabel.SetTextColor(rarityColor, ComponentState.Normal);
                }
                else
                {
                    _selectedRarityLabel.SetTextColor(Color.White, ComponentState.Normal);
                }
            }
            else
            {
                _selectedRarityLabel.IsVisibleInParent = false;
            }

            _quantityInput.Enable();
            _priceInput.Enable();
            _clearSlotButton.Enable();

            var max = Math.Max(1, listing.MaxQuantity);
            _quantityInput.SetRange(1, max);
            _quantityInput.Value = Math.Clamp(listing.DesiredQuantity, 1, max);

            // Usar double.MaxValue en vez de NaN
            _priceInput.SetRange(1, double.MaxValue);
            _priceInput.Value = Math.Max(1, listing.PricePerUnit);
            _selectedPricePreviewLabel.Text = Strings.PlayerShops.BrowserPriceEach.ToString(listing.PricePerUnit);
        }
        else
        {
            _selectedLabel.Text = Strings.PlayerShops.SelectSlot;
            _selectedRarityLabel.IsVisibleInParent = false;
            _selectedDescriptionLabel.Text = Strings.PlayerShops.Hint;
            _selectedPricePreviewLabel.Text = Strings.PlayerShops.BrowserPriceEach.ToString(0);
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

    private void OnDecorationSelected(string? decorationName)
    {
        _selectedDecoration = string.IsNullOrWhiteSpace(decorationName)
            ? PlayerShopEntityConstants.DefaultDecoration
            : decorationName;
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
        _selectedPricePreviewLabel.Text = Strings.PlayerShops.BrowserPriceEach.ToString(_selectedSlot.PricePerUnit);
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

        PacketSender.SendCreatePlayerShop(name, payloads, _selectedDecoration);
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
        PopulateDecorationSelector();
        UpdateMerchantModeButtonState();
        Show();
    }

    private void PopulateDecorationSelector()
    {
        _decorationSelector.ClearItems();

        var options = GameContentManager.Current.GetTextureNames(TextureType.Entity)
            .Where(name =>
            {
                var normalized = name.Replace('\\', '/');
                return normalized.StartsWith("stores/", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (options.Count == 0)
        {
            options.Add(PlayerShopEntityConstants.DefaultDecoration);
        }

        foreach (var option in options)
        {
            var displayName = Path.GetFileName(GameContentManager.RemoveExtension(option));
            displayName = string.IsNullOrWhiteSpace(displayName) ? option : displayName;
            _decorationSelector.AddItem(displayName, option, option);
        }

        var selected = options.FirstOrDefault(
            option => option.Equals(_selectedDecoration, StringComparison.OrdinalIgnoreCase)
        ) ?? options.First();

        _selectedDecoration = string.IsNullOrWhiteSpace(selected)
            ? PlayerShopEntityConstants.DefaultDecoration
            : selected;

        _decorationSelector.SelectByUserData(_selectedDecoration);
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

        // Si quieres obligar a que tenga nombre, descomenta:
        // if (string.IsNullOrWhiteSpace(_shopNameInput.Text))
        // {
        //     return false;
        // }

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
