using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Admin.Actions;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Enums;
using Intersect.Network.Packets.Localization;
using static Intersect.Client.Framework.File_Management.GameContentManager;

namespace Intersect.Client.Interface.Game.Admin;

public sealed class AdminItemManagementWindow : Window
{
    private readonly IFont? _defaultFont;
    private readonly TextBox _playerNameInput;
    private readonly LabeledComboBox _itemDropdown;
    private readonly TextBoxNumeric _quantityInput;
    private readonly LabeledCheckBox _overflowCheckbox;
    private readonly LabeledCheckBox _reserveCheckbox;
    private readonly Button _giveItemButton;
    private readonly Button _spawnItemButton;
    private bool _localizationSubscribed;

    public AdminItemManagementWindow() : base(
        Interface.GameUi.GameCanvas,
        Strings.AdminWindow.ItemManagement,
        false,
        nameof(AdminItemManagementWindow)
    )
    {
        _defaultFont = Skin?.DefaultFont ?? Current.GetFont(TitleLabel.FontName);

        IsResizable = false;
        MinimumSize = new Point(460, 320);
        InnerPanelPadding = new Padding(8);
        InnerPanel.DockChildSpacing = new Padding(0, 8, 0, 0);

        var contentPanel = new Panel(this, "ContentPanel")
        {
            Dock = Pos.Fill,
            ShouldDrawBackground = false,
            DockChildSpacing = new Padding(0, 8, 0, 0),
        };

        _ = new Label(contentPanel, "PlayerNameLabel")
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.Name,
        };

        _playerNameInput = new TextBox(contentPanel, nameof(_playerNameInput))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            PlaceholderText = Strings.AdminWindow.NamePlaceholder,
        };
        Interface.FocusComponents.Add(_playerNameInput);
        _playerNameInput.TextChanged += (_, _) => UpdateActionControls();

        _itemDropdown = new LabeledComboBox(contentPanel, nameof(_itemDropdown))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Label = Strings.AdminWindow.Item,
            TextPadding = new Padding(8, 4, 0, 4),
        };
        PopulateItemDropdown(requestLocalization: true);
        _itemDropdown.ItemSelected += (_, _) => UpdateActionControls();

        var quantityPanel = new Panel(contentPanel, "QuantityPanel")
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
        };

        _ = new Label(quantityPanel, "QuantityLabel")
        {
            Dock = Pos.Left,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 0, 4, 0),
            Text = Strings.AdminWindow.Quantity,
            TextAlign = Pos.Left | Pos.CenterV,
        };

        _quantityInput = new TextBoxNumeric(quantityPanel, nameof(_quantityInput))
        {
            Dock = Pos.Fill,
            Font = _defaultFont,
            FontSize = 12,
            Padding = new Padding(8, 4),
        };
        _quantityInput.SetRange(1, int.MaxValue);
        _quantityInput.Value = 1;
        _quantityInput.ValueChanged += (_, _) => UpdateActionControls();
        Interface.FocusComponents.Add(_quantityInput);

        _overflowCheckbox = new LabeledCheckBox(contentPanel, nameof(_overflowCheckbox))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
            Text = Strings.AdminWindow.AllowOverflow,
        };

        _reserveCheckbox = new LabeledCheckBox(contentPanel, nameof(_reserveCheckbox))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
            Text = Strings.AdminWindow.ReserveSpawn,
        };

        var buttonsPanel = new Panel(contentPanel, "ButtonsPanel")
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
            DockChildSpacing = new Padding(8, 0, 0, 0),
        };

        _giveItemButton = new Button(buttonsPanel, nameof(_giveItemButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.GiveItem,
        };
        StyleButton(_giveItemButton);
        _giveItemButton.Clicked += GiveItemButtonOnClicked;

        _spawnItemButton = new Button(buttonsPanel, nameof(_spawnItemButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.SpawnItem,
        };
        StyleButton(_spawnItemButton);
        _spawnItemButton.Clicked += SpawnItemButtonOnClicked;

        UpdateActionControls();
        SubscribeToLocalizationUpdates();
    }

    private string PlayerName => _playerNameInput.Text?.Trim() ?? string.Empty;

    private static Padding StdPad(int x = 8, int y = 4) => new(x, y);

    private void StyleButton(Button button)
    {
        button.MinimumSize = new Point(120, 28);
        button.Padding = StdPad();
        button.Margin = new Margin(0, 0, 8, 0);
        button.Font = _defaultFont;
        button.FontSize = 12;
    }

    private void PopulateItemDropdown(bool requestLocalization)
    {
        var selectedItemId = _itemDropdown.SelectedItem?.UserData as Guid?;
        _itemDropdown.ClearItems();

        var noneItem = _itemDropdown.AddItem(Strings.AdminWindow.None, userData: Guid.Empty);

        foreach (var descriptor in ItemDescriptor.Lookup.Values
                     .OfType<ItemDescriptor>()
                     .OrderBy(
                         descriptor => GetLocalizedItemName(descriptor),
                         StringComparer.CurrentCultureIgnoreCase
                     ))
        {
            if (descriptor == null)
            {
                continue;
            }

            var displayName = GetLocalizedItemName(descriptor);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = descriptor.Id.ToString();
            }

            _ = _itemDropdown.AddItem(displayName, userData: descriptor.Id);
        }

        _itemDropdown.SelectedItem = noneItem;
        if (selectedItemId is { } selectedId)
        {
            _itemDropdown.SelectByUserData(selectedId);
        }

        if (requestLocalization)
        {
            RequestLocalizationEntries();
        }
    }

    private static string GetLocalizedItemName(ItemDescriptor descriptor) =>
        GameLocalization.GetTextOrDefault(
            descriptor.Type.ToString(),
            descriptor.Id,
            "Name",
            descriptor.Name ?? string.Empty
        );

    private void RequestLocalizationEntries()
    {
        var requests = new List<LocalizationRequestEntry>();
        foreach (var descriptor in ItemDescriptor.Lookup.Values.OfType<ItemDescriptor>())
        {
            if (descriptor == null)
            {
                continue;
            }

            requests.Add(new LocalizationRequestEntry(descriptor.Type.ToString(), descriptor.Id.ToString(), "Name"));
        }

        if (requests.Count > 0)
        {
            GameLocalization.RequestEntries(requests);
        }
    }

    private void SubscribeToLocalizationUpdates()
    {
        if (_localizationSubscribed)
        {
            return;
        }

        GameLocalization.LocalizedTextsUpdated += OnLocalizedTextsUpdated;
        Disposed += (_, _) => UnsubscribeFromLocalizationUpdates();
        _localizationSubscribed = true;
    }

    private void UnsubscribeFromLocalizationUpdates()
    {
        if (!_localizationSubscribed)
        {
            return;
        }

        GameLocalization.LocalizedTextsUpdated -= OnLocalizedTextsUpdated;
        _localizationSubscribed = false;
    }

    private void OnLocalizedTextsUpdated(string language, IReadOnlyCollection<LocalizationRequestEntry> requests)
    {
        var itemType = GameObjectType.Item.ToString();
        if (!requests.Any(request => request.EntityType == itemType && request.Field == "Name"))
        {
            return;
        }

        PopulateItemDropdown(requestLocalization: false);
    }

    private bool TryGetItemActionParameters(out string playerName, out Guid itemId, out int quantity)
    {
        playerName = PlayerName;
        if (playerName.Length == 0)
        {
            itemId = Guid.Empty;
            quantity = 0;
            return false;
        }

        if (!(_itemDropdown.SelectedItem?.UserData is Guid selectedItemId) || selectedItemId == Guid.Empty)
        {
            itemId = Guid.Empty;
            quantity = 0;
            return false;
        }

        quantity = (int)Math.Max(1, Math.Round(_quantityInput.Value));
        itemId = selectedItemId;
        return true;
    }

    private void GiveItemButtonOnClicked(Base sender, MouseButtonState args)
    {
        if (!TryGetItemActionParameters(out var playerName, out var itemId, out var quantity))
        {
            return;
        }

        PacketSender.SendAdminAction(
            new GiveItemAction(playerName, itemId, quantity, _overflowCheckbox.IsChecked)
        );
    }

    private void SpawnItemButtonOnClicked(Base sender, MouseButtonState args)
    {
        if (!TryGetItemActionParameters(out var playerName, out var itemId, out var quantity))
        {
            return;
        }

        PacketSender.SendAdminAction(
            new SpawnItemAction(playerName, itemId, quantity, _reserveCheckbox.IsChecked)
        );
    }

    private void UpdateActionControls()
    {
        var hasPlayer = PlayerName.Length > 0;
        var quantityValid = _quantityInput.Value >= 1;
        var hasItem = _itemDropdown.SelectedItem?.UserData is Guid selectedItemId && selectedItemId != Guid.Empty;

        var shouldEnable = hasPlayer && quantityValid && hasItem;

        _giveItemButton.IsDisabled = !shouldEnable;
        _spawnItemButton.IsDisabled = !shouldEnable;
    }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(UI.InGame, Graphics.Renderer?.GetResolutionString(), saveOutput: true);
    }
}
