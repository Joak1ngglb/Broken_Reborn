using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.Component;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Network.Packets.Shops;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopBrowseWindow
{
    private readonly WindowControl _window;
    private readonly Label _ownerLabel;
    private readonly ScrollControl _itemsScroll;
    private readonly Label _statusLabel;
    private readonly Label _emptyLabel;
    private readonly List<PlayerShopBrowseItemRow> _rows = new();

    private ShopSnapshot _snapshot;

    public event Action<PlayerShopBrowseWindow>? Closed;

    public PlayerShopBrowseWindow(Canvas parent, ShopSnapshot snapshot)
    {
        _snapshot = snapshot;

        _window = new WindowControl(parent, Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name), false, nameof(PlayerShopBrowseWindow));
        _window.SetSize(700, 520);
        _window.DisableResizing();
        _window.Closed += WindowOnClosed;
        _window.Disposed += WindowOnClosed;

        _ownerLabel = new Label(_window, "PlayerShopOwnerLabel")
        {
            Text = Strings.PlayerShops.BrowserOwner.ToString(snapshot.OwnerName),
        };
        _ownerLabel.SetBounds(20, 30, 400, 24);

        _itemsScroll = new ScrollControl(_window, "PlayerShopItemsScroll");
        _itemsScroll.SetBounds(20, 60, 660, 380);
        _itemsScroll.EnableScroll(false, true);

        _emptyLabel = new Label(_itemsScroll, "PlayerShopEmptyLabel")
        {
            Text = Strings.PlayerShops.BrowserEmpty,
            Alignment = [Alignments.Center],
            TextColor = Color.Gray,
        };
        _emptyLabel.Dock = Pos.Fill;

        _statusLabel = new Label(_window, "PlayerShopStatusLabel")
        {
            Text = Strings.PlayerShops.BrowseStatus,
        };
        _statusLabel.SetBounds(20, 460, 400, 24);
        _statusLabel.SetTextColor(Color.White, ComponentState.Normal);

        BuildRows();
    }

    internal int RowWidth => Math.Max(600, _itemsScroll.Width - 20);

    private void WindowOnClosed(Base? sender, EventArgs args)
    {
        Closed?.Invoke(this);
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
        _window.Title = Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name);
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
        _statusLabel.SetTextColor(Color.LightGreen, ComponentState.Normal);
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
