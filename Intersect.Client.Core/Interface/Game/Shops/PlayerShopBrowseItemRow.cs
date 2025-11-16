using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Network.Packets.Shops;

namespace Intersect.Client.Interface.Game.Shops;

public sealed class PlayerShopBrowseItemRow : Base
{
    private readonly PlayerShopBrowseWindow _owner;
    private readonly ImagePanel _icon;
    private readonly Label _nameLabel;
    private readonly Label _priceLabel;
    private readonly Label _availableLabel;
    private readonly Label _totalLabel;
    private readonly TextBoxNumeric _quantityInput;
    private readonly Button _buyButton;

    private PlayerShopItemSnapshot _snapshot;

    public PlayerShopBrowseItemRow(PlayerShopBrowseWindow owner, Base parent, PlayerShopItemSnapshot snapshot)
        : base(parent, nameof(PlayerShopBrowseItemRow))
    {
        _owner = owner;
        _snapshot = snapshot;

        SetSize(_owner.RowWidth, 70);
        Dock = Pos.Top;
        Margin = new Margin(0, 0, 0, 6);

        _icon = new ImagePanel(this, "PlayerShopBrowseIcon")
        {
           
            Size = new Point(32, 32),
            Margin = new Margin(6, 6, 10, 6),
        };

        _nameLabel = new Label(this, "PlayerShopBrowseName")
        {
            Text = Strings.PlayerShops.UnknownItem,
        };
        _nameLabel.SetBounds(70, 6, 240, 20);

        _priceLabel = new Label(this, "PlayerShopBrowsePrice")
        {
            Text = Strings.PlayerShops.BrowserPriceEach.ToString(snapshot.PricePerUnit),
        };
        _priceLabel.SetBounds(70, 28, 200, 18);

        _availableLabel = new Label(this, "PlayerShopBrowseAvailable")
        {
            Text = Strings.PlayerShops.BrowserAvailable.ToString(snapshot.Quantity),
        };
        _availableLabel.SetBounds(70, 48, 200, 18);

        _quantityInput = new TextBoxNumeric(this, "PlayerShopBrowseQuantity")
        {
            Minimum = 1,
        };
        _quantityInput.SetBounds(300, 20, 80, 26);

        // Clamp y actualización de total sin comportamientos raros
        _quantityInput.ValueChanged += (_, args) =>
        {
            var raw = (int)args.Value;
            var max = Math.Max(1, _snapshot.Quantity);
            var clamped = Math.Clamp(raw, 1, max);

            if (clamped != raw)
            {
                _quantityInput.Value = clamped;
            }

            UpdateTotal(clamped);
        };

        _totalLabel = new Label(this, "PlayerShopBrowseTotal")
        {
            Text = Strings.PlayerShops.BrowserTotal.ToString(snapshot.PricePerUnit),
        };
        _totalLabel.SetBounds(390, 20, 120, 26);

        _buyButton = new Button(this, "PlayerShopBrowseBuy")
        {
            Text = Strings.PlayerShops.BrowserBuy,
        };
        _buyButton.SetBounds(520, 18, 100, 32);
        _buyButton.Clicked += (_, _) => _owner.RequestPurchase(this);
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
        UpdateRow();
    }

    public PlayerShopItemSnapshot Snapshot => _snapshot;

    public int RequestedQuantity =>
        Math.Clamp((int)_quantityInput.Value, 1, Math.Max(1, _snapshot.Quantity));


    private void UpdateRow()
    {
        // Nombre + icono del item
        if (!ItemDescriptor.TryGet(_snapshot.ItemId, out var descriptor))
        {
            _nameLabel.Text = Strings.PlayerShops.UnknownItem;
            _icon.Texture = null;
        }
        else
        {
            _nameLabel.Text = descriptor.Name ?? Strings.PlayerShops.UnknownItem;
            var texture = GameContentManager.Current.GetTexture(
                Framework.Content.TextureType.Item,
                descriptor.Icon
            );
            _icon.Texture = texture;
            _icon.RenderColor = descriptor.Color;
        }

        // Datos de precio y cantidad
        _priceLabel.Text = Strings.PlayerShops.BrowserPriceEach.ToString(_snapshot.PricePerUnit);
        _availableLabel.Text = Strings.PlayerShops.BrowserAvailable.ToString(_snapshot.Quantity);

        var max = Math.Max(1, _snapshot.Quantity);
        _quantityInput.SetRange(1, max);

        if (_snapshot.Quantity <= 0)
        {
            // Sin stock -> deshabilitar compra y mostrar total en 0
            _quantityInput.Value = 1;
            _quantityInput.Disable();

            _buyButton.Disable();
            _totalLabel.Text = Strings.PlayerShops.BrowserTotal.ToString(0);
        }
        else
        {
            // Con stock -> habilitar compra y ajustar cantidad si estaba fuera de rango
            if (_quantityInput.Value < 1 || _quantityInput.Value > _snapshot.Quantity)
            {
                _quantityInput.Value = 1;
            }

            _quantityInput.Enable();
            _buyButton.Enable();

            UpdateTotal(RequestedQuantity);
        }
    }

    private void UpdateTotal(int quantity)
    {
        if (_snapshot.Quantity <= 0)
        {
            _totalLabel.Text = Strings.PlayerShops.BrowserTotal.ToString(0);
            return;
        }

        var clamped = Math.Clamp(quantity, 1, Math.Max(1, _snapshot.Quantity));
        _totalLabel.Text = Strings.PlayerShops.BrowserTotal.ToString(clamped * _snapshot.PricePerUnit);
    }
}
