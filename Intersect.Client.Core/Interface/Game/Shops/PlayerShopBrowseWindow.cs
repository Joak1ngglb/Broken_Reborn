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

namespace Intersect.Client.Interface.Game.Shops
{
    public sealed class PlayerShopBrowseWindow : Window
    {
        private const int RowHeight = 70;

        private Label _ownerLabel;
        private ScrollControl _itemsScroll;
        private Label _statusLabel;
        private Label _emptyLabel;
        private readonly List<PlayerShopBrowseItemRow> _rows = new();

        private bool _uiInitialized;

        private ShopSnapshot _snapshot;

        public PlayerShopBrowseWindow(Canvas parent, ShopSnapshot snapshot)
            : base(parent, Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name), false, nameof(PlayerShopBrowseWindow))
        {
            _snapshot = snapshot;

            // Ventana más compacta
            SetSize(640, 430);
            DisableResizing();

            InitializeUi();
        }

        internal int RowWidth => Math.Max(520, _itemsScroll.Width - 20);

        private void InitializeUi()
        {
            if (_uiInitialized)
            {
                return;
            }

            _uiInitialized = true;

            // Propietario de la tienda
            _ownerLabel = new Label(this, "PlayerShopOwnerLabel")
            {
                Text = Strings.PlayerShops.BrowserOwner.ToString(_snapshot.OwnerName),
            };
            _ownerLabel.SetBounds(16, 28, 400, 22);

            // Scroll de items
            _itemsScroll = new ScrollControl(this, "PlayerShopItemsScroll");
            _itemsScroll.SetBounds(16, 56, 608, 300);
            _itemsScroll.EnableScroll(false, true);

            _emptyLabel = new Label(_itemsScroll, "PlayerShopEmptyLabel")
            {
                Text = Strings.PlayerShops.BrowserEmpty,
                Alignment = [Alignments.Center],
                TextColor = Color.Gray,
            };
            _emptyLabel.Dock = Pos.Fill;

            // Estado / mensajes (abajo)
            _statusLabel = new Label(this, "PlayerShopStatusLabel")
            {
                Text = Strings.PlayerShops.BrowseStatus,
            };
            _statusLabel.SetBounds(16, 370, 480, 24);
            _statusLabel.SetTextColor(Color.White, ComponentState.Normal);

           
            LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer?.GetResolutionString());
            BuildRows();
        }

        protected override void EnsureInitialized()
        {
            InitializeUi();
        }

        /// <summary>
        /// Reconstruye las filas ajustando el scroll al nuevo tamaño
        /// para evitar huecos visuales cuando se quitan ítems.
        /// </summary>
        private void BuildRows()
        {
            if (_itemsScroll == null)
            {
                return;
            }

            var scrollBar = _itemsScroll.VerticalScrollBar;

            // === 1. Capturar índice de la primera fila visible (ancla) ===
            int previousFirstIndex = 0;
            if (scrollBar != null && _rows.Count > 0)
            {
                var oldContentHeight = _rows.Count * RowHeight;
                var viewHeight = _itemsScroll.Height;
                var denom = Math.Max(0, oldContentHeight - viewHeight);

                if (denom > 0)
                {
                    var scrollPixels = (int)(scrollBar.ScrollAmount * denom);
                    previousFirstIndex = Math.Clamp(scrollPixels / RowHeight, 0, Math.Max(0, _rows.Count - 1));
                }
            }

            // === 2. Limpiar filas anteriores ===
            foreach (var row in _rows)
            {
                row.Dispose();
            }
            _rows.Clear();

            if (_snapshot.Items == null || _snapshot.Items.Count == 0)
            {
                _emptyLabel.IsVisibleInParent = true;

                // Sin items -> resetear scroll y tamaño interno
                if (scrollBar != null)
                {
                    scrollBar.ScrollAmount = 0f;
                    scrollBar.ContentSize = 0;
                    scrollBar.ViewableContentSize = _itemsScroll.Height;
                }

                _itemsScroll.SetInnerSize(_itemsScroll.Width - 16, 0);
                _itemsScroll.UpdateScrollBars();

                return;
            }

            _emptyLabel.IsVisibleInParent = false;

            // === 3. Crear filas nuevas ===
            foreach (var item in _snapshot.Items)
            {
                var row = new PlayerShopBrowseItemRow(this, _itemsScroll, item)
                {
                    Width = RowWidth,
                };
                _rows.Add(row);
            }

            // === 4. Ajustar tamaño interno y scroll ===
            var newCount = _rows.Count;
            var innerHeight = newCount * RowHeight;

            _itemsScroll.SetInnerSize(_itemsScroll.Width - 16, innerHeight);
            _itemsScroll.UpdateScrollBars();

            if (scrollBar != null)
            {
                scrollBar.ContentSize = innerHeight;
                scrollBar.ViewableContentSize = _itemsScroll.Height;

                // Recalcular índice de la primera fila visible
                previousFirstIndex = Math.Clamp(previousFirstIndex, 0, Math.Max(0, newCount - 1));

                var denom = Math.Max(0, innerHeight - _itemsScroll.Height);
                float newScrollAmount;

                if (denom > 0)
                {
                    var newOffset = previousFirstIndex * RowHeight;
                    newScrollAmount = (float)newOffset / denom;
                }
                else
                {
                    newScrollAmount = 0f;
                }

                if (newScrollAmount < 0f)
                {
                    newScrollAmount = 0f;
                }
                else if (newScrollAmount > 1f)
                {
                    newScrollAmount = 1f;
                }

                scrollBar.ScrollAmount = newScrollAmount;
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
}
