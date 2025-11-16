using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Network.Packets.Shops;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopBrowseWindow : Window
{
    private Label _ownerLabel;
    private ScrollControl _itemsScroll;
    private Label _statusLabel;
    private  Label _emptyLabel;
    private readonly List<PlayerShopBrowseItemRow> _rows = new();

    private bool _uiInitialized;

    private ShopSnapshot _snapshot;

    public PlayerShopBrowseWindow(Canvas parent, ShopSnapshot snapshot)
        : base(parent, Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name), false, nameof(PlayerShopBrowseWindow))
    {
        _snapshot = snapshot;

        SetSize(700, 520);
        DisableResizing();

        InitializeUi();
    }

    internal int RowWidth => Math.Max(600, _itemsScroll.Width - 20);

    private void InitializeUi()
    {
        if (_uiInitialized)
        {
            return;
        }

        _uiInitialized = true;

        _ownerLabel = new Label(this, "PlayerShopOwnerLabel")
        {
            Text = Strings.PlayerShops.BrowserOwner.ToString(_snapshot.OwnerName),
        };
        _ownerLabel.SetBounds(20, 30, 400, 24);

        _itemsScroll = new ScrollControl(this, "PlayerShopItemsScroll");
        _itemsScroll.SetBounds(20, 60, 660, 380);
        _itemsScroll.EnableScroll(false, true);

        _emptyLabel = new Label(_itemsScroll, "PlayerShopEmptyLabel")
        {
            Text = Strings.PlayerShops.BrowserEmpty,
            Alignment = [Alignments.Center],
            TextColor = Color.Gray,
        };
        _emptyLabel.Dock = Pos.Fill;

        _statusLabel = new Label(this, "PlayerShopStatusLabel")
        {
            Text = Strings.PlayerShops.BrowseStatus,
        };
        _statusLabel.SetBounds(20, 460, 400, 24);
        _statusLabel.SetTextColor(Color.White, ComponentState.Normal);

        BuildRows();

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer?.GetResolutionString());
    }

    protected override void EnsureInitialized()
    {
        InitializeUi();
    }

    private void BuildRows()
    {
        foreach (var row in _rows)
        {
            row.Dispose();
        }
        _rows.Clear();

        if (_snapshot.Items.Count == 0)
        {
            _emptyLabel.IsVisibleInParent = true;
            return;
        }

        _emptyLabel.IsVisibleInParent = false;

        foreach (var item in _snapshot.Items)
        {
            var row = new PlayerShopBrowseItemRow(this, _itemsScroll, item)
            {
                Width = RowWidth,
            };
            _rows.Add(row);
        }
    }

    public void UpdateSnapshot(ShopSnapshot snapshot)
    {
        _snapshot = snapshot;
        Title = Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name);
        _ownerLabel.Text = Strings.PlayerShops.BrowserOwner.ToString(snapshot.OwnerName);
        BuildRows();
    }

    public void Update()
    {
        foreach (var row in _rows)
        {
            row.Width = RowWidth;
        }
    }

    internal void RequestPurchase(PlayerShopBrowseItemRow row)
    {
        var quantity = row.RequestedQuantity;
        if (quantity <= 0)
        {
            _statusLabel.Text = Strings.PlayerShops.ErrorInvalidQuantity;
            _statusLabel.SetTextColor(Color.OrangeRed, ComponentState.Normal);
            return;
        }

        PacketSender.SendBuyPlayerShopItem(_snapshot.ShopId, row.Snapshot.ShopItemId, quantity);
        _statusLabel.Text = Strings.PlayerShops.StatusSubmitting;
        _statusLabel.SetTextColor(Color.ForestGreen, ComponentState.Normal);
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
