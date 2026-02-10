using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Network.Packets.Shops;

namespace Intersect.Client.Interface.Game.Shops
{
    public sealed class PlayerShopBrowseItemRow : Base
    {
        private readonly PlayerShopBrowseWindow _owner;

        private readonly ImagePanel _cardPanel;
        private readonly ImagePanel _iconPanel;
        private readonly Label _nameLabel;
        private readonly Label _rarityLabel;
        private readonly Label _priceLabel;
        private readonly Label _availableLabel;
        private readonly Label _totalLabel;
        private readonly TextBoxNumeric _quantityInput;
        private readonly Button _buyButton;

        private ItemDescriptor? _descriptor;
        private PlayerShopItemSnapshot _snapshot;

        public PlayerShopItemSnapshot Snapshot => _snapshot;

        public int RequestedQuantity =>
            Math.Clamp((int)_quantityInput.Value, 1, Math.Max(1, _snapshot.Quantity));

        public PlayerShopBrowseItemRow(PlayerShopBrowseWindow owner, Base parent, PlayerShopItemSnapshot snapshot)
            : base(parent, nameof(PlayerShopBrowseItemRow))
        {
            _owner = owner;
            _snapshot = snapshot;

            SetSize(_owner.RowWidth, 82);
            Dock = Pos.None;
            Margin = new Margin(0, 0, 0, 6);

            _cardPanel = new ImagePanel(this, "PlayerShopBrowseCard")
            {
                Texture = Graphics.Renderer?.WhitePixel,
                RenderColor = new Color(28, 34, 46, 230),
            };
            _cardPanel.SetBounds(0, 0, _owner.RowWidth, 82);

            _iconPanel = new ImagePanel(this, "PlayerShopBrowseIcon");
            _iconPanel.SetBounds(10, 10, 56, 56);
            _iconPanel.HoverEnter += OnHoverEnter;
            _iconPanel.HoverLeave += OnHoverLeave;
            _iconPanel.Clicked += OnIconClick;

            _nameLabel = new Label(this, "PlayerShopBrowseName")
            {
                Text = Strings.PlayerShops.UnknownItem,
                FontName = "sourcesansproblack",
            };
            _nameLabel.SetBounds(78, 8, 260, 20);

            _rarityLabel = new Label(this, "PlayerShopBrowseRarity")
            {
                Text = string.Empty,
                BackgroundTemplateName = "quantity.png",
                Padding = new Padding(4, 2, 4, 2),
            };
            _rarityLabel.SetBounds(78, 30, 120, 18);

            _priceLabel = new Label(this, "PlayerShopBrowsePrice")
            {
                Text = Strings.PlayerShops.BrowserPriceEach.ToString(snapshot.PricePerUnit),
                BackgroundTemplateName = "quantity.png",
                TextColor = new Color(255, 235, 120),
                Padding = new Padding(6, 2, 6, 2),
                FontName = "sourcesansproblack",
            };
            _priceLabel.SetBounds(206, 30, 190, 18);

            _availableLabel = new Label(this, "PlayerShopBrowseAvailable")
            {
                Text = Strings.PlayerShops.BrowserAvailable.ToString(snapshot.Quantity),
                BackgroundTemplateName = "quantity.png",
                Padding = new Padding(6, 2, 6, 2),
            };
            _availableLabel.SetBounds(78, 52, 180, 18);

            _quantityInput = new TextBoxNumeric(this, "PlayerShopBrowseQuantity")
            {
                Minimum = 1,
            };
            _quantityInput.SetBounds(438, 24, 90, 30);
            _quantityInput.ValueChanged += QuantityInputOnValueChanged;

            _totalLabel = new Label(this, "PlayerShopBrowseTotal")
            {
                Text = Strings.PlayerShops.BrowserTotal.ToString(snapshot.PricePerUnit),
                FontName = "sourcesansproblack",
            };
            _totalLabel.SetBounds(538, 26, 170, 24);

            _buyButton = new Button(this, "PlayerShopBrowseBuy")
            {
                Text = Strings.PlayerShops.BrowserBuy,
            };
            _buyButton.SetBounds(714, 20, 110, 36);
            _buyButton.Clicked += (_, _) => _owner.RequestPurchase(this);
            LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer?.GetResolutionString());
            UpdateRow();
        }

        public void UpdateSnapshot(PlayerShopItemSnapshot snapshot)
        {
            _snapshot = snapshot;
            UpdateRow();
        }

        private void UpdateRow()
        {
            _cardPanel.SetBounds(0, 0, Width, Height);

            if (!ItemDescriptor.TryGet(_snapshot.ItemId, out _descriptor))
            {
                _descriptor = null;
                _nameLabel.Text = Strings.PlayerShops.UnknownItem;
                _iconPanel.Texture = null;
                _iconPanel.IsVisibleInParent = false;
                _rarityLabel.IsVisibleInParent = false;
            }
            else
            {
                _nameLabel.Text = GetLocalizedItemName(_descriptor);

                if (_descriptor.Rarity > 0)
                {
                    _rarityLabel.Text = Strings.GetLocalizedItemRarityName(_descriptor.Rarity);
                    _rarityLabel.IsVisibleInParent = true;

                    if (CustomColors.Items.Rarities.TryGetValue(_descriptor.Rarity, out var rarityColor))
                    {
                        _rarityLabel.SetTextColor(rarityColor, ComponentState.Normal);
                    }
                }
                else
                {
                    _rarityLabel.IsVisibleInParent = false;
                }

                var tex = GameContentManager.Current.GetTexture(
                    Framework.Content.TextureType.Item,
                    _descriptor.Icon
                );
                _iconPanel.Texture = tex;
                if (tex != null)
                {
                    _iconPanel.RenderColor = _descriptor.Color;
                    _iconPanel.IsVisibleInParent = true;
                }
                else
                {
                    _iconPanel.IsVisibleInParent = false;
                }
            }

            _priceLabel.Text = Strings.PlayerShops.BrowserPriceEach.ToString(_snapshot.PricePerUnit);
            _availableLabel.Text = Strings.PlayerShops.BrowserAvailable.ToString(_snapshot.Quantity);

            var max = Math.Max(1, _snapshot.Quantity);
            _quantityInput.SetRange(1, max);

            if (_snapshot.Quantity <= 0)
            {
                _quantityInput.Value = 1;
                _quantityInput.Disable();
                _buyButton.Disable();
                _totalLabel.Text = Strings.PlayerShops.BrowserTotal.ToString(0);
            }
            else
            {
                if (_quantityInput.Value < 1 || _quantityInput.Value > _snapshot.Quantity)
                {
                    _quantityInput.Value = 1;
                }

                _quantityInput.Enable();
                _buyButton.Enable();

                UpdateTotal(RequestedQuantity);
            }
        }

        public void RefreshLocalization()
        {
            UpdateRow();
        }

        private static string GetLocalizedItemName(ItemDescriptor descriptor) =>
            GameLocalization.GetTextOrDefault(
                descriptor.Type.ToString(),
                descriptor.Id,
                "Name",
                descriptor.Name ?? Strings.PlayerShops.UnknownItem
            );

        private void QuantityInputOnValueChanged(TextBoxNumeric sender, ValueChangedEventArgs<double> args)
        {
            var raw = (int)args.Value;
            var max = Math.Max(1, _snapshot.Quantity);
            var clamped = Math.Clamp(raw, 1, max);

            if (Math.Abs(sender.Value - clamped) > double.Epsilon)
            {
                sender.Value = clamped;
            }

            UpdateTotal(clamped);
        }

        private void UpdateTotal(int quantity)
        {
            if (_snapshot.Quantity <= 0)
            {
                _totalLabel.Text = Strings.PlayerShops.BrowserTotal.ToString(0);
                return;
            }

            var clamped = Math.Clamp(quantity, 1, Math.Max(1, _snapshot.Quantity));
            var total = clamped * _snapshot.PricePerUnit;
            _totalLabel.Text = Strings.PlayerShops.BrowserTotal.ToString(total);
        }

        private void OnIconClick(Base sender, MouseButtonState args)
        {
            if (args.MouseButton is not MouseButton.Left)
            {
                return;
            }

            _owner.RequestPurchase(this);
        }

        private void OnHoverEnter(Base sender, EventArgs args)
        {
            if (_descriptor == null)
            {
                return;
            }

            Interface.GameUi.ItemDescriptionWindow ??= new ItemDescriptionWindow();
            Interface.GameUi.ItemDescriptionWindow.Show(
                _descriptor,
                _snapshot.Quantity,
                null
            );
        }

        private void OnHoverLeave(Base sender, EventArgs args)
        {
            Interface.GameUi.ItemDescriptionWindow?.Hide();
        }

        public void SetBuying(bool on)
        {
            _buyButton.IsDisabled = on || _snapshot.Quantity <= 0;
            _quantityInput.IsDisabled = on || _snapshot.Quantity <= 0;
        }

        public void DetachEvents()
        {
            _iconPanel.HoverEnter -= OnHoverEnter;
            _iconPanel.HoverLeave -= OnHoverLeave;
            _iconPanel.Clicked -= OnIconClick;
            _quantityInput.ValueChanged -= QuantityInputOnValueChanged;
        }
    }
}
