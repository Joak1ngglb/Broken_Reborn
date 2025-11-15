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
using Intersect.Client.Interface;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Core;
using Intersect.Framework.Core.GameObjects.Maps.MapList;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;
using static Intersect.Client.Framework.File_Management.GameContentManager;

namespace Intersect.Client.Interface.Game.Admin;

public partial class AdminWindow : Window
{
    private readonly Panel _contentPanel;

    private readonly LabeledComboBox _accessDropdown;

    private readonly Panel _accessPanel;
    private readonly Button _accessSetButton;

    private readonly Panel _actionPanel;
    private readonly Table _actionTable;
    private readonly Button _openItemWindowButton;
    private readonly Button _openMailWindowButton;
    private readonly ScrollControl _leftColumn;
    private readonly TabControl _leftTabs;
    private readonly Button _banButton;
    private readonly IFont? _defaultFont;
    private readonly TexturePicker _faceTexturePicker;
    private readonly Button _kickPlayerButton;
    private readonly Button _killPlayerButton;
    private readonly Button _leaveInstanceButton;
    private readonly Button _mailBroadcastButton;
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

    private readonly Button _spawnItemButton;
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
        var appearTab = AddTab(Strings.AdminWindow.Appearance);

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

        _spawnItemButton = new Button(_actionPanel, nameof(_spawnItemButton))
        {
            Font = _defaultFont,
            FontSize = 12,
            MinimumSize = new Point(120, 0),
            Padding = new Padding(8, 4),
            Text = Strings.AdminWindow.SpawnItem,
        };
        _spawnItemButton.Clicked += SpawnItemButtonOnClicked;

        _mailBroadcastButton = new Button(_actionPanel, nameof(_mailBroadcastButton))
        {
            Font = _defaultFont,
            FontSize = 12,
            MinimumSize = new Point(120, 0),
            Padding = new Padding(8, 4),
            Text = Strings.AdminWindow.MailBroadcast,
        };
        _mailBroadcastButton.Clicked += MailBroadcastButtonOnClicked;

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
            _spawnItemButton,
            _mailBroadcastButton,
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

        #region Additional Interfaces

        var externalInterfacesSection = new Panel(actionsTab, "ExternalInterfacesSection")
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
            DockChildSpacing = new Padding(0, 8, 0, 0),
        };

        _ = new Label(externalInterfacesSection, "ExternalInterfacesLabel")
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.AdditionalInterfaces,
        };

        var externalButtonsPanel = new Panel(externalInterfacesSection, "ExternalButtonsPanel")
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
        };

        _openItemWindowButton = new Button(externalButtonsPanel, nameof(_openItemWindowButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.ItemManagement,
        };
        StyleButton(_openItemWindowButton);

        _openItemWindowButton.Clicked += SpawnItemButtonOnClicked;
        _openMailWindowButton = new Button(externalButtonsPanel, nameof(_openMailWindowButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.MailBroadcast,
        };
        StyleButton(_openMailWindowButton);

        _openMailWindowButton.Clicked += MailBroadcastButtonOnClicked;
        externalInterfacesSection.SizeToChildren(recursive: true);

        #endregion Additional Interfaces

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



    protected override void EnsureInitialized()
    {
        InnerPanel.SizeToChildren(recursive: true);

        LoadJsonUi(UI.InGame, Graphics.Renderer?.GetResolutionString(), saveOutput: true);

        // Asegura layout correcto aunque el JSON no tenga todo
        ForcePostJsonLayout();

        UpdateMapList();
    }

    #region Action Handlers
 private void SpawnItemButtonOnClicked(Base sender, MouseButtonState e)
    {
        Interface.Interface.GameUi.OpenAdminItemManagementWindow();
    }

    private void MailBroadcastButtonOnClicked(Base sender, MouseButtonState e)
    {
        Interface.Interface.GameUi.OpenAdminMailBroadcastWindow();
    }

 private void SpawnItemButtonOnClicked(Base sender, MouseButtonState e)
    {
        Interface.Interface.GameUi.OpenAdminItemManagementWindow();
    }

    private void MailBroadcastButtonOnClicked(Base sender, MouseButtonState e)
    {
        Interface.Interface.GameUi.OpenAdminMailBroadcastWindow();
    }

 private void SpawnItemButtonOnClicked(Base sender, MouseButtonState e)
    {
        Interface.GameUi.OpenAdminItemManagementWindow();
    }

    private void MailBroadcastButtonOnClicked(Base sender, MouseButtonState e)
    {
        Interface.GameUi.OpenAdminMailBroadcastWindow();
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