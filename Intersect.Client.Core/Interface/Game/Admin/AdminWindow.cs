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
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps.MapList;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;
using static Intersect.Client.Framework.File_Management.GameContentManager;

namespace Intersect.Client.Interface.Game.Admin;

public partial class AdminWindow : Window
{
    private const int MailAttachmentSlotCount = BroadcastMailAction.MaxAttachments;

    private readonly Panel _contentPanel;

    private readonly LabeledComboBox _accessDropdown;

    private readonly Panel _accessPanel;
    private readonly Button _accessSetButton;

    private readonly Panel _actionPanel;
    private readonly Table _actionTable;
    private readonly Panel _itemActionPanel;
    private readonly LabeledComboBox _itemDropdown;
    private readonly Panel _itemQuantityPanel;
    private readonly Label _itemQuantityLabel;
    private readonly TextBoxNumeric _itemQuantityInput;
    private readonly LabeledCheckBox _itemOverflowCheckbox;
    private readonly LabeledCheckBox _itemReserveCheckbox;
    private readonly Panel _itemButtonsPanel;
    private readonly Button _giveItemButton;
    private readonly Button _spawnItemButton;
    private readonly ScrollControl _leftColumn;
    private readonly TabControl _leftTabs;
    private readonly Panel _mailBroadcastPanel;
    private readonly Label _mailBroadcastHeader;
    private readonly Label _mailSubjectLabel;
    private readonly TextBox _mailSubjectInput;
    private readonly Label _mailMessageLabel;
    private readonly MultilineTextBox _mailMessageInput;
    private readonly LabeledComboBox[] _mailAttachmentDropdowns;
    private readonly TextBoxNumeric[] _mailAttachmentQuantityInputs;
    private readonly LabeledCheckBox _mailOnlineOnlyCheckbox;
    private readonly Panel _mailButtonsPanel;
    private readonly Button _mailSendButton;
    private readonly Button _banButton;
    private readonly IFont? _defaultFont;
    private readonly TexturePicker _faceTexturePicker;
    private readonly Button _kickPlayerButton;
    private readonly Button _killPlayerButton;
    private readonly Button _leaveInstanceButton;
    private readonly Label _mapListLabel;

    private readonly Panel _mapListPanel;
    private readonly Panel _mapListPanelHeader;
    private readonly Panel _mapTreeContainer;
    private readonly LabeledCheckBox _mapSortCheckbox;
    private readonly Button _muteButton;
    private readonly TextBox _nameInput;
    private readonly Label _nameLabel;
    private readonly Panel? _leftHeader;
    private readonly Panel _namePanel;

    private readonly TexturePicker _spriteTexturePicker;
    private readonly Button _unbanButton;
    private readonly Button _unmuteButton;
    private readonly Button _warpMeToPlayerButton;
    private readonly Button _warpPlayerToMeButton;

    private BanMuteBox? _banOrMuteWindow;
    private TreeControl? _mapTree;

    public AdminWindow(Base gameCanvas) : base(
        gameCanvas,
        Strings.AdminWindow.Title,
        false,
        nameof(AdminWindow)
    )
    {
        _defaultFont = Skin?.DefaultFont ?? Current.GetFont(TitleLabel.FontName);

        IsResizable = false;
        TitleLabel.FontSize = 14;

        MinimumSize = new Point(720, 600);
        InnerPanelPadding = new Padding(8);
        InnerPanel.DockChildSpacing = new Padding(8);

        _contentPanel = new Panel(this, nameof(_contentPanel))
        {
            Dock = Pos.Fill,
            ShouldDrawBackground = false,
            DockChildSpacing = new Padding(8),
        };

        _leftColumn = new ScrollControl(_contentPanel, nameof(_leftColumn))
        {
            AutoHideBars = true,
            Dock = Pos.Left,
            InnerPanelPadding = Padding.Zero,
            Margin = new Margin(0, 0, 8, 0),
            ShouldDrawBackground = false,
            Width = 360,
        };
        _leftColumn.SetOverflow(OverflowBehavior.Hidden, OverflowBehavior.Auto);

        _leftTabs = new TabControl(_leftColumn, nameof(_leftTabs))
        {
            Dock = Pos.Fill,
            ShouldDrawBackground = false,
        };

        ScrollControl AddTab(string name)
        {
            var page = _leftTabs.AddPage(name).Page;
            page.Dock = Pos.Fill;

            var scroll = new ScrollControl(page, $"{name}Scroll")
            {
                Dock = Pos.Fill,
                AutoHideBars = true,
                ShouldDrawBackground = false,
                InnerPanelPadding = Padding.Zero,
            };
            scroll.SetOverflow(OverflowBehavior.Hidden, OverflowBehavior.Auto);
            scroll.InnerPanel.DockChildSpacing = new Padding(0, 12, 0, 0);
            return scroll;
        }

        var actionsTab = AddTab(Strings.AdminWindow.QuickActions);
        var itemsTab = AddTab(Strings.AdminWindow.ItemManagement);
        var mailTab = AddTab(Strings.AdminWindow.MailBroadcast);
        var appearTab = AddTab(Strings.AdminWindow.Appearance);

        _mailAttachmentDropdowns = new LabeledComboBox[MailAttachmentSlotCount];
        _mailAttachmentQuantityInputs = new TextBoxNumeric[MailAttachmentSlotCount];

        #region Name Input

        _namePanel = new Panel(actionsTab, nameof(_namePanel))
        {
            Dock = Pos.Top, ShouldDrawBackground = false,
        };

        _nameLabel = new Label(_namePanel, nameof(_nameLabel))
        {
            Dock = Pos.Left,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 0, 4, 0),
            Text = Strings.AdminWindow.Name,
            TextAlign = Pos.Right,
        };
        _nameInput = new TextBox(_namePanel, nameof(_nameInput))
        {
            Font = _defaultFont,
            FontSize = 12,
            Dock = Pos.Fill,
            PlaceholderText = Strings.AdminWindow.NamePlaceholder,
        };
        Interface.FocusComponents.Add(_nameInput);
        _nameInput.TextChanged += (_, _) => UpdateItemActionControls();

        #endregion Name Input

        #region Access

        _accessPanel = new Panel(actionsTab, nameof(_accessPanel))
        {
            Dock = Pos.Top, ShouldDrawBackground = false,
        };

        _accessSetButton = new Button(_accessPanel, nameof(_accessSetButton))
        {
            Dock = Pos.Right,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(4, 0, 0, 0),
            Padding = new Padding(8, 4),
            Text = Strings.AdminWindow.SetPower,
        };
        _accessSetButton.Clicked += AccessSetButtonOnClicked;

        _accessDropdown = new LabeledComboBox(_accessPanel, nameof(_accessDropdown))
        {
            AutoSizeToContents = false,
            Dock = Pos.Fill,
            Font = _defaultFont,
            FontSize = 12,
            TextPadding = new Padding(8, 4, 0, 4),
            Label = Strings.AdminWindow.Access,
        };
        _ = _accessDropdown.AddItem(Strings.General.None, userData: "None");
        _ = _accessDropdown.AddItem(Strings.AdminWindow.Access1, userData: "Moderator");
        _ = _accessDropdown.AddItem(Strings.AdminWindow.Access2, userData: "Admin");

        #endregion Access

        #region Quick Admin Actions

        var quickActionsSection = new Panel(actionsTab, "QuickActionsSection")
        {
            Dock = Pos.Top,
            DockChildSpacing = new Padding(0, 8, 0, 0),
            ShouldDrawBackground = false,
        };

        _ = new Label(quickActionsSection, "QuickActionsLabel")
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.QuickActions,
        };

        _actionPanel = new Panel(quickActionsSection, nameof(_actionPanel))
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
        };

        _actionTable = new Table(_actionPanel, nameof(_actionTable))
        {
            CellSpacing = new Point(8, 8),
            ColumnCount = 3,
            Dock = Pos.Top,
            FitRowHeightToContents = true,
            Font = _defaultFont,
            FontSize = 12,
            SizeToContents = true,
        };
        _actionTable.AutoSizeToContentWidth = true;
        _actionTable.AutoSizeToContentHeight = true;
        _actionTable.AutoSizeToContentWidthOnChildResize = true;
        _actionTable.AutoSizeToContentHeightOnChildResize = true;

        _warpMeToPlayerButton = new Button(_actionPanel, nameof(_warpMeToPlayerButton))
        {
            Text = Strings.AdminWindow.WarpMeToPlayer,
        };
        StyleButton(_warpMeToPlayerButton);
        _warpMeToPlayerButton.Clicked += WarpMeToPlayerButtonOnClicked;

        _kickPlayerButton = new Button(_actionPanel, nameof(_kickPlayerButton))
        {
            Text = Strings.AdminWindow.KickPlayer,
        };
        StyleButton(_kickPlayerButton);
        _kickPlayerButton.Clicked += KickPlayerButtonOnClicked;

        _killPlayerButton = new Button(_actionPanel, nameof(_killPlayerButton))
        {
            Text = Strings.AdminWindow.KillPlayer,
        };
        StyleButton(_killPlayerButton);
        _killPlayerButton.Clicked += KillPlayerButtonOnClicked;

        _warpPlayerToMeButton = new Button(_actionPanel, nameof(_warpPlayerToMeButton))
        {
            Text = Strings.AdminWindow.WarpPlayerToMe,
        };
        StyleButton(_warpPlayerToMeButton);
        _warpPlayerToMeButton.Clicked += WarpPlayerToMeButtonOnClicked;

        _muteButton = new Button(_actionPanel, nameof(_muteButton))
        {
            Text = Strings.AdminWindow.Mute,
        };
        StyleButton(_muteButton);
        _muteButton.Clicked += MuteButtonOnClicked;

        _unmuteButton = new Button(_actionPanel, nameof(_unmuteButton))
        {
            Text = Strings.AdminWindow.Unmute,
        };
        StyleButton(_unmuteButton);
        _unmuteButton.Clicked += UnmuteButtonOnClicked;

        _leaveInstanceButton = new Button(_actionPanel, nameof(_leaveInstanceButton))
        {
            Text = Strings.AdminWindow.LeaveInstance,
        };
        StyleButton(_leaveInstanceButton);
        _leaveInstanceButton.Clicked += LeaveInstanceButtonOnClicked;

        _banButton = new Button(_actionPanel, nameof(_banButton))
        {
            Text = Strings.AdminWindow.Ban,
        };
        StyleButton(_banButton);
        _banButton.Clicked += BanButtonOnClicked;

        _unbanButton = new Button(_actionPanel, nameof(_unbanButton))
        {
            Text = Strings.AdminWindow.Unban,
        };
        StyleButton(_unbanButton);
        _unbanButton.Clicked += UnbanButtonOnClicked;

        _actionTable.AddCells(
            _warpMeToPlayerButton,
            _kickPlayerButton,
            _killPlayerButton,
            _warpPlayerToMeButton,
            _muteButton,
            _unmuteButton,
            _leaveInstanceButton,
            _banButton,
            _unbanButton
        );

#if DEBUG
        foreach (var button in new[]
                 {
                     _warpMeToPlayerButton,
                     _kickPlayerButton,
                     _killPlayerButton,
                     _warpPlayerToMeButton,
                     _muteButton,
                     _unmuteButton,
                     _leaveInstanceButton,
                     _banButton,
                     _unbanButton,
                 })
        {
            button.IsDisabled = false;
        }
#endif

        _actionPanel.SizeToChildren(recursive: true);
        quickActionsSection.SizeToChildren(recursive: true);

        #endregion Quick Admin Actions

        #region Item Actions

        var itemActionsSection = new Panel(itemsTab, "ItemActionsSection")
        {
            Dock = Pos.Top,
            DockChildSpacing = new Padding(0, 8, 0, 0),
            ShouldDrawBackground = false,
        };

        _ = new Label(itemActionsSection, "ItemActionsLabel")
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.ItemManagement,
        };

        _itemActionPanel = new Panel(itemActionsSection, nameof(_itemActionPanel))
        {
            Dock = Pos.Top,
            DockChildSpacing = new Padding(0, 4, 0, 0),
            ShouldDrawBackground = false,
        };

        _itemDropdown = new LabeledComboBox(_itemActionPanel, nameof(_itemDropdown))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            TextPadding = new Padding(8, 4, 0, 4),
            Label = Strings.AdminWindow.Item,
        };

        PopulateItemDropdown(_itemDropdown);
        _itemDropdown.ItemSelected += (_, _) => UpdateItemActionControls();

        _itemQuantityPanel = new Panel(_itemActionPanel, nameof(_itemQuantityPanel))
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
            Margin = new Margin(0, 4, 0, 0),
        };

        _itemQuantityLabel = new Label(_itemQuantityPanel, nameof(_itemQuantityLabel))
        {
            Dock = Pos.Left,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 0, 4, 0),
            Text = Strings.AdminWindow.Quantity,
            TextAlign = Pos.Left | Pos.CenterV,
        };

        _itemQuantityInput = new TextBoxNumeric(_itemQuantityPanel, nameof(_itemQuantityInput))
        {
            Dock = Pos.Fill,
            Font = _defaultFont,
            FontSize = 12,
            Padding = new Padding(8, 4),
        };
        _itemQuantityInput.SetRange(1, int.MaxValue);
        _itemQuantityInput.Value = 1;
        _itemQuantityInput.ValueChanged += (_, _) => UpdateItemActionControls();
        Interface.FocusComponents.Add(_itemQuantityInput);

        _itemOverflowCheckbox = new LabeledCheckBox(_itemActionPanel, nameof(_itemOverflowCheckbox))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
            Text = Strings.AdminWindow.AllowOverflow,
        };

        _itemReserveCheckbox = new LabeledCheckBox(_itemActionPanel, nameof(_itemReserveCheckbox))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
            Text = Strings.AdminWindow.ReserveSpawn,
        };

        _itemButtonsPanel = new Panel(_itemActionPanel, nameof(_itemButtonsPanel))
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
            Margin = new Margin(0, 4, 0, 0),
        };

        _giveItemButton = new Button(_itemButtonsPanel, nameof(_giveItemButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.GiveItem,
        };
        StyleButton(_giveItemButton);
        _giveItemButton.Clicked += GiveItemButtonOnClicked;

        _spawnItemButton = new Button(_itemButtonsPanel, nameof(_spawnItemButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.SpawnItem,
        };
        StyleButton(_spawnItemButton);

        _spawnItemButton.Clicked += SpawnItemButtonOnClicked;

        itemActionsSection.SizeToChildren(recursive: true);

#if DEBUG
        _giveItemButton.IsDisabled = false;
        _spawnItemButton.IsDisabled = false;
#endif

        #endregion Item Actions

        #region Mail Broadcast

        _mailBroadcastPanel = new Panel(mailTab, nameof(_mailBroadcastPanel))
        {
            Dock = Pos.Top,
            DockChildSpacing = new Padding(0, 4, 0, 0),
            ShouldDrawBackground = false,
        };

        _mailBroadcastHeader = new Label(_mailBroadcastPanel, nameof(_mailBroadcastHeader))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.MailBroadcast,
        };

        _mailSubjectLabel = new Label(_mailBroadcastPanel, nameof(_mailSubjectLabel))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
            Text = Strings.AdminWindow.MailSubject,
        };

        _mailSubjectInput = new TextBox(_mailBroadcastPanel, nameof(_mailSubjectInput))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
        };
        Interface.FocusComponents.Add(_mailSubjectInput);
        _mailSubjectInput.TextChanged += (_, _) => UpdateItemActionControls();

        _mailMessageLabel = new Label(_mailBroadcastPanel, nameof(_mailMessageLabel))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
            Text = Strings.AdminWindow.MailMessage,
        };

        _mailMessageInput = new MultilineTextBox(_mailBroadcastPanel)
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
        };
        _mailMessageInput.Name = nameof(_mailMessageInput);
        _mailMessageInput.Height = 140;
        _mailMessageInput.TextChanged += (_, _) => UpdateItemActionControls();
        Interface.FocusComponents.Add(_mailMessageInput);

        for (var index = 0; index < MailAttachmentSlotCount; index++)
        {
            var dropdown = new LabeledComboBox(
                _mailBroadcastPanel,
                $"{nameof(_mailAttachmentDropdowns)}{index}"
            )
            {
                Dock = Pos.Top,
                Font = _defaultFont,
                FontSize = 12,
                Margin = new Margin(0, 4, 0, 0),
                TextPadding = new Padding(8, 4, 0, 4),
                Label = Strings.AdminWindow.MailAttachmentItem.ToString(index + 1),
            };
            PopulateItemDropdown(dropdown);
            dropdown.ItemSelected += (_, _) => UpdateItemActionControls();
            Interface.FocusComponents.Add(dropdown);
            _mailAttachmentDropdowns[index] = dropdown;

            var quantityPanel = new Panel(
                _mailBroadcastPanel,
                $"{nameof(_mailAttachmentQuantityInputs)}Panel{index}"
            )
            {
                Dock = Pos.Top,
                ShouldDrawBackground = false,
                Margin = new Margin(0, 4, 0, 0),
            };

            _ = new Label(quantityPanel, $"{nameof(_mailAttachmentQuantityInputs)}Label{index}")
            {
                Dock = Pos.Left,
                Font = _defaultFont,
                FontSize = 12,
                Margin = new Margin(0, 0, 4, 0),
                Text = Strings.AdminWindow.MailAttachmentQuantity.ToString(index + 1),
                TextAlign = Pos.Left | Pos.CenterV,
            };

            var quantityInput = new TextBoxNumeric(
                quantityPanel,
                $"{nameof(_mailAttachmentQuantityInputs)}{index}"
            )
            {
                Dock = Pos.Fill,
                Font = _defaultFont,
                FontSize = 12,
                Padding = new Padding(8, 4),
            };
            quantityInput.SetRange(1, int.MaxValue);
            quantityInput.Value = 1;
            quantityInput.ValueChanged += (_, _) => UpdateItemActionControls();
            Interface.FocusComponents.Add(quantityInput);
            _mailAttachmentQuantityInputs[index] = quantityInput;
        }

        _mailOnlineOnlyCheckbox = new LabeledCheckBox(_mailBroadcastPanel, nameof(_mailOnlineOnlyCheckbox))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(0, 4, 0, 0),
            Text = Strings.AdminWindow.MailOnlineOnly,
        };

        _mailButtonsPanel = new Panel(_mailBroadcastPanel, nameof(_mailButtonsPanel))
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
            Margin = new Margin(0, 4, 0, 0),
        };

        _mailSendButton = new Button(_mailButtonsPanel, nameof(_mailSendButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.MailSend,
        };
        StyleButton(_mailSendButton);
        _mailSendButton.Clicked += SendMailBroadcastButtonOnClicked;

        _mailBroadcastPanel.SizeToChildren(recursive: true);

#if DEBUG
        _mailSendButton.IsDisabled = false;
#endif

        #endregion Mail Broadcast

        #region Sprite/Face Pickers

        var appearanceSection = new Panel(appearTab, "AppearanceSection")
        {
            Dock = Pos.Top,
            DockChildSpacing = new Padding(0, 8, 0, 0),
            ShouldDrawBackground = false,
        };

        _ = new Label(appearanceSection, "AppearanceLabel")
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.Appearance,
        };

        _spriteTexturePicker = new TexturePicker(appearanceSection, nameof(_spriteTexturePicker))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            ButtonText = Strings.AdminWindow.SetSprite,
            LabelText = Strings.AdminWindow.Sprite,
            TextureType = TextureType.Entity,
        };
        _spriteTexturePicker.Submitted += SpriteTexturePickerOnSubmitted;

        _faceTexturePicker = new TexturePicker(appearanceSection, nameof(_faceTexturePicker))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            ButtonText = Strings.AdminWindow.SetFace,
            LabelText = Strings.AdminWindow.Face,
            Margin = new Margin(0, 4, 0, 0),
            TextureType = TextureType.Face,
        };
        _faceTexturePicker.Submitted += FaceTexturePickerOnSubmitted;

        appearanceSection.SizeToChildren(recursive: true);

        #endregion Sprite/Face Pickers

        #region Map List

        _mapListPanel = new Panel(_contentPanel, nameof(_mapListPanel))
        {
            Dock = Pos.Fill,
            DockChildSpacing = new Padding(0, 8, 0, 0),
            ShouldDrawBackground = false,
        };

        _mapListPanelHeader = new Panel(_mapListPanel, nameof(_mapListPanelHeader))
        {
            Dock = Pos.Top,
            DockChildSpacing = new Padding(8, 0, 0, 0),
            ShouldDrawBackground = false,
        };

        _mapListLabel = new Label(_mapListPanelHeader, nameof(_mapListLabel))
        {
            Dock = Pos.Left,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.MapList,
        };

        _mapSortCheckbox = new LabeledCheckBox(_mapListPanelHeader, nameof(_mapSortCheckbox))
        {
            Dock = Pos.Right,
            Font = _defaultFont,
            FontSize = 12,
            Margin = new Margin(8, 0, 0, 0),
            Text = Strings.AdminWindow.SortMapList,
            TooltipText = Strings.AdminWindow.SortMapListTooltip,
            TooltipFont = _defaultFont,
            TooltipFontSize = 12,
        };

        _mapSortCheckbox.CheckChanged += MapSortCheckboxOnCheckChanged;

        _mapTreeContainer = new Panel(_mapListPanel, nameof(_mapTreeContainer))
        {
            Dock = Pos.Fill,
            ShouldDrawBackground = false,
        };

        #endregion Map List

        UpdateItemActionControls();

        SkipRender();
    }

    private static Padding StdPad(int x = 8, int y = 4) => new Padding(x, y);

    private void StyleButton(Button button)
    {
        button.MinimumSize = new Point(120, 28);
        button.Padding = StdPad();
        button.Margin = new Margin(0, 0, 8, 0);
        button.Font = _defaultFont;
        button.FontSize = 12;
    }

    private void PopulateItemDropdown(LabeledComboBox dropdown)
    {
        dropdown.ClearItems();

        var noneItem = dropdown.AddItem(Strings.AdminWindow.None, userData: Guid.Empty);

        foreach (var descriptor in ItemDescriptor.Lookup.Values
                     .OfType<ItemDescriptor>()
                     .OrderBy(descriptor => descriptor?.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
        {
            if (descriptor == null)
            {
                continue;
            }

            var displayName = descriptor.Name;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = descriptor.Id.ToString();
            }

            _ = dropdown.AddItem(displayName, userData: descriptor.Id);
        }

        dropdown.SelectedItem = noneItem;
    }

    protected override void EnsureInitialized()
    {
        InnerPanel.SizeToChildren(recursive: true);

        LoadJsonUi(UI.InGame, Graphics.Renderer?.GetResolutionString(), saveOutput: true);

        // Asegura layout correcto aunque el JSON no tenga todo
        ForcePostJsonLayout();

        UpdateMapList();
    }

    #region Action Handlers

    private bool TryGetItemActionParameters(out string playerName, out Guid itemId, out int quantity)
    {
        playerName = PlayerName ?? string.Empty;
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

        quantity = (int)Math.Max(1, Math.Round(_itemQuantityInput.Value));
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
            new GiveItemAction(playerName, itemId, quantity, _itemOverflowCheckbox.IsChecked)
        );
    }

    private void SpawnItemButtonOnClicked(Base sender, MouseButtonState args)
    {
        if (!TryGetItemActionParameters(out var playerName, out var itemId, out var quantity))
        {
            return;
        }

        PacketSender.SendAdminAction(
            new SpawnItemAction(playerName, itemId, quantity, _itemReserveCheckbox.IsChecked)
        );
    }

    private void SendMailBroadcastButtonOnClicked(Base sender, MouseButtonState args)
    {
        var subject = _mailSubjectInput?.Text?.Trim() ?? string.Empty;
        var message = _mailMessageInput?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var attachments = CollectMailBroadcastAttachments();
        if (attachments.Count == 0)
        {
            return;
        }

        PacketSender.SendAdminAction(
            new BroadcastMailAction(
                subject,
                message,
                attachments,
                _mailOnlineOnlyCheckbox?.IsChecked ?? false
            )
        );
    }

    private List<BroadcastMailAttachment> CollectMailBroadcastAttachments()
    {
        var attachments = new List<BroadcastMailAttachment>();

        for (var index = 0; index < _mailAttachmentDropdowns.Length; index++)
        {
            var dropdown = _mailAttachmentDropdowns[index];
            var quantityInput = _mailAttachmentQuantityInputs[index];

            if (!(dropdown?.SelectedItem?.UserData is Guid itemId) || itemId == Guid.Empty)
            {
                continue;
            }

            var quantity = (int)Math.Max(1, Math.Round(quantityInput?.Value ?? 0));
            if (quantity <= 0)
            {
                continue;
            }

            attachments.Add(new BroadcastMailAttachment(itemId, quantity));
        }

        return attachments;
    }

    private void UnbanButtonOnClicked(Base s, MouseButtonState e)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        _ = new InputBox(
            Strings.AdminWindow.UnbanCaption.ToString(args: playerName),
            Strings.AdminWindow.UnbanPrompt.ToString(args: playerName),
            InputType.YesNo,
            (_, _) => PacketSender.SendAdminAction(new UnbanAction(playerName))
        );
    }

    private void UnmuteButtonOnClicked(Base s, MouseButtonState e)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        _ = new InputBox(
            Strings.AdminWindow.UnmuteCaption.ToString(args: playerName),
            Strings.AdminWindow.UnmutePrompt.ToString(args: playerName),
            InputType.YesNo,
            (_, _) => PacketSender.SendAdminAction(new UnmuteAction(playerName))
        );
    }

    private void WarpPlayerToMeButtonOnClicked(Base @base, MouseButtonState mouseButtonState)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        PacketSender.SendAdminAction(new WarpToMeAction(playerName));
    }

    private void KillPlayerButtonOnClicked(Base @base, MouseButtonState mouseButtonState)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        PacketSender.SendAdminAction(new KillAction(playerName));
    }

    private void KickPlayerButtonOnClicked(Base @base, MouseButtonState mouseButtonState)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        PacketSender.SendAdminAction(new KickAction(playerName));
    }

    private void WarpMeToPlayerButtonOnClicked(Base @base, MouseButtonState mouseButtonState)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        PacketSender.SendAdminAction(new WarpMeToAction(playerName));
    }

    public string? PlayerName
    {
        get => _nameInput.Text?.Trim();
        set
        {
            _nameInput.Text = value;
            UpdateItemActionControls();
        }
    }

    private void FaceTexturePickerOnSubmitted(TexturePicker sender, ValueChangedEventArgs<string?> arguments)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        var textureName = arguments.Value?.Trim();
        PacketSender.SendAdminAction(new SetFaceAction(playerName, textureName));
    }

    private void SpriteTexturePickerOnSubmitted(TexturePicker sender, ValueChangedEventArgs<string?> arguments)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        var textureName = arguments.Value?.Trim();
        PacketSender.SendAdminAction(new SetSpriteAction(playerName, textureName));
    }

    private void LeaveInstanceButtonOnClicked(Base @base, MouseButtonState mouseButtonState)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        PacketSender.SendAdminAction(new ReturnToOverworldAction(playerName));
    }

    private void AccessSetButtonOnClicked(Base @base, MouseButtonState mouseButtonState)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        var power = _accessDropdown.SelectedItem?.UserData?.ToString()?.Trim();
        if (power is null or { Length: < 1 })
        {
            return;
        }

        PacketSender.SendAdminAction(new SetAccessAction(playerName, power));
    }

    private void BanButtonOnClicked(Base sender, MouseButtonState arguments)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        if (string.Equals(playerName, Globals.Me?.Name, StringComparison.CurrentCultureIgnoreCase))
        {
            return;
        }

        _banOrMuteWindow = new BanMuteBox(
            Strings.AdminWindow.BanCaption.ToString(playerName),
            Strings.AdminWindow.BanPrompt.ToString(playerName),
            (_, _) =>
            {
                PacketSender.SendAdminAction(
                    new BanAction(
                        playerName,
                        _banOrMuteWindow?.GetDuration() ?? 0,
                        _banOrMuteWindow?.GetReason() ?? string.Empty,
                        _banOrMuteWindow?.BanIp() ?? false
                    )
                );

                _banOrMuteWindow?.Dispose();
            }
        );
    }

    private void MuteButtonOnClicked(Base sender, MouseButtonState arguments)
    {
        if (PlayerName is not { Length: > 0 } playerName)
        {
            return;
        }

        if (string.Equals(playerName, Globals.Me?.Name, StringComparison.CurrentCultureIgnoreCase))
        {
            return;
        }

        _banOrMuteWindow = new BanMuteBox(
            Strings.AdminWindow.MuteCaption.ToString(playerName),
            Strings.AdminWindow.MutePrompt.ToString(playerName),
            (_, _) =>
            {
                PacketSender.SendAdminAction(
                    new MuteAction(
                        playerName,
                        _banOrMuteWindow?.GetDuration() ?? 0,
                        _banOrMuteWindow?.GetReason() ?? string.Empty,
                        _banOrMuteWindow?.BanIp() ?? false
                    )
                );

                _banOrMuteWindow?.Dispose();
            }
        );
    }

    private void UpdateItemActionControls()
    {
        if (_giveItemButton != null && _spawnItemButton != null)
        {
            var hasPlayer = PlayerName is { Length: > 0 };
            var quantityValid = _itemQuantityInput?.Value >= 1;
            var hasItem = _itemDropdown?.SelectedItem?.UserData is Guid selectedItemId && selectedItemId != Guid.Empty;

            var shouldEnable = hasPlayer && quantityValid && hasItem;

            _giveItemButton.IsDisabled = !shouldEnable;
            _spawnItemButton.IsDisabled = !shouldEnable;
        }

        if (_mailSendButton != null)
        {
            var hasSubject = !string.IsNullOrWhiteSpace(_mailSubjectInput?.Text);
            var hasMessage = !string.IsNullOrWhiteSpace(_mailMessageInput?.Text);
            var hasAttachmentItem = false;
            var quantitiesValid = true;

            for (var index = 0; index < _mailAttachmentDropdowns.Length; index++)
            {
                var dropdown = _mailAttachmentDropdowns[index];
                var quantityInput = _mailAttachmentQuantityInputs[index];

                if (!(dropdown?.SelectedItem?.UserData is Guid attachmentItemId) || attachmentItemId == Guid.Empty)
                {
                    continue;
                }

                hasAttachmentItem = true;

                if ((quantityInput?.Value ?? 0) < 1)
                {
                    quantitiesValid = false;
                    break;
                }
            }

            _mailSendButton.IsDisabled = !(hasSubject && hasMessage && hasAttachmentItem && quantitiesValid);
        }
    }

    private void MapSortCheckboxOnCheckChanged(ICheckbox sender, ValueChangedEventArgs<bool> eventArgs)
    {
        UpdateMapList();
    }

    public void UpdateMapList()
    {
        _mapTree?.DelayedDelete();

        _mapTree = new TreeControl(_mapTreeContainer, nameof(_mapTree))
        {
            Dock = Pos.Fill,
            Font = _defaultFont,
            FontSize = 12,
        };

        _mapTree.SelectionChanged += MapTreeSelectionChanged;

        AddMapListToTree(MapList.List, _mapTree);
    }

    private void AddMapListToTree(MapList mapList, TreeNode parent)
    {
        if (_mapSortCheckbox.IsChecked)
        {
            foreach (var mapListMap in MapList.OrderedMaps)
            {
                _ = parent.AddNode(mapListMap.Name, mapListMap.MapId);
            }

            return;
        }

        foreach (var item in mapList.Items)
        {
            switch (item)
            {
                case MapListFolder folder:
                    AddMapListToTree(folder.Children, parent.AddNode(item.Name, folder));
                    break;
                case MapListMap map:
                    parent.AddNode(item.Name, map.MapId);
                    break;
            }
        }
    }

    private static void MapTreeSelectionChanged(Base sender, EventArgs arguments)
    {
        if (sender is not TreeNode treeNode)
        {
            ApplicationContext.Context.Value?.Logger.LogDebug(
                "MapList selection triggered by a sender of type {SenderType} instead of a {TreeNodeType}",
                sender.GetType().GetName(true),
                typeof(TreeNode).GetName(true)
            );
            return;
        }

        if (!treeNode.IsSelected)
        {
            // We don't care about unselected nodes
            return;
        }

        if (treeNode is not { UserData: Guid mapId } || mapId == default)
        {
            if (treeNode.UserData is MapListFolder folder)
            {
                ApplicationContext.Context.Value?.Logger.LogInformation(
                    "Selected map list folder '{FolderName}' ({FolderId}) ({ChildrenCount} direct children)",
                    folder.Name,
                    folder.FolderId,
                    folder.Children.Items.Count
                );
            }
            else
            {
                ApplicationContext.Context.Value?.Logger.LogDebug(
                    "Selected non-map map list node '{TreeNodeText}'",
                    treeNode.Text
                );
            }

            return;
        }

        if (Globals.Me?.MapId == mapId)
        {
            ApplicationContext.CurrentContext.Logger.LogInformation(
                "Ignoring warp to map '{MapName}' ({MapId}) because the player is already on the map",
                treeNode.Text,
                mapId
            );
            return;
        }

        PacketSender.SendAdminAction(new WarpToMapAction(mapId));
    }
    /// <summary>
    /// Reaplica un layout seguro después de cargar/mergear el JSON.
    /// Garantiza que los contenedores críticos queden con Dock/Width/Margins correctos,
    /// aun cuando el JSON no defina o cambie estos valores.
    /// </summary>
    private void ForcePostJsonLayout()
    {
        // Columna izquierda
        if (_leftColumn != null)
        {
            _leftColumn.Dock = Pos.Left;
            _leftColumn.Width = 360;
            _leftColumn.Margin = new Margin(0, 0, 8, 0);
            _leftColumn.ShouldDrawBackground = false;
            _leftColumn.SetOverflow(OverflowBehavior.Hidden, OverflowBehavior.Auto);
            // Asegura que su InnerPanel no “coma” espacio inesperado
            _leftColumn.InnerPanelPadding = Padding.Zero;
        }

        // Header fijo
        if (_leftHeader != null)
        {
            _leftHeader.Dock = Pos.Top;
            _leftHeader.DockChildSpacing = new Padding(0, 12, 0, 0);
            _leftHeader.ShouldDrawBackground = false;
        }

        // Tabs
        if (_leftTabs != null)
        {
            _leftTabs.Dock = Pos.Fill;
            _leftTabs.ShouldDrawBackground = false;
            _leftTabs.Font ??= _defaultFont;
            if (_leftTabs.FontSize <= 0) _leftTabs.FontSize = 12;
        }

        // Panel de mapas a la derecha
        if (_mapListPanel != null)
        {
            _mapListPanel.Dock = Pos.Fill;
            _mapListPanel.ShouldDrawBackground = false;
        }
        if (_mapListPanelHeader != null)
        {
            _mapListPanelHeader.Dock = Pos.Top;
            _mapListPanelHeader.ShouldDrawBackground = false;
        }
        if (_mapTreeContainer != null)
        {
            _mapTreeContainer.Dock = Pos.Fill;
            _mapTreeContainer.ShouldDrawBackground = false;
        }

        // Recalcular tamaños y disposición
        _contentPanel?.SizeToChildren(recursive: true);
        InnerPanel?.SizeToChildren(recursive: true);
        SizeToChildren(recursive: true);
    }

    #endregion
}