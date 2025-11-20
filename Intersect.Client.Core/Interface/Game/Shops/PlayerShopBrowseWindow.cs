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
        /// Reconstruye las filas.
        /// Solo se llama cuando cambia la estructura (slots añadidos/eliminados o reordenados).
        /// Resetea el scroll a la parte superior para evitar comportamientos raros.
        /// </summary>
        private void BuildRows()
        {
            if (_itemsScroll == null)
            {
                return;
            }

            var scrollBar = _itemsScroll.VerticalScrollBar;

            // Limpiar filas anteriores
            foreach (var row in _rows)
            {
                row.Dispose();
            }
            _rows.Clear();

            var items = _snapshot.Items ?? new List<PlayerShopItemSnapshot>();

            if (items.Count == 0)
            {
                _emptyLabel.IsVisibleInParent = true;

                LayoutRows();
                if (scrollBar != null)
                {
                    scrollBar.ScrollAmount = 0f;
                }

                return;
            }

            _emptyLabel.IsVisibleInParent = false;

            // Crear filas nuevas (Gwen se encarga del layout interno)
            foreach (var item in items)
            {
                var row = new PlayerShopBrowseItemRow(this, _itemsScroll, item)
                {
                    Width = RowWidth,
                };
                _rows.Add(row);
            }

            LayoutRows();

            // Como cambió la estructura, ponemos el scroll arriba para evitar huecos locos
            if (scrollBar != null)
            {
                scrollBar.ScrollAmount = 0f;
            }
        }

        public void UpdateSnapshot(ShopSnapshot snapshot)
        {
            _snapshot = snapshot;

            Title = Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name);
            _ownerLabel.Text = Strings.PlayerShops.BrowserOwner.ToString(snapshot.OwnerName);

            if (!_uiInitialized || _itemsScroll == null)
            {
                BuildRows();
                return;
            }

            var items = _snapshot.Items ?? new List<PlayerShopItemSnapshot>();

            // 1) Detectar si la estructura cambió (slots nuevos/eliminados o reordenados)
            var structureChanged = false;

            if (_rows.Count != items.Count)
            {
                structureChanged = true;
            }
            else
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (_rows[i].Snapshot.ShopItemId != items[i].ShopItemId)
                    {
                        structureChanged = true;
                        break;
                    }
                }
            }

            // 2) Si la estructura ES la misma → solo actualizamos filas (cantidad, precio, etc.)
            if (!structureChanged)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    _rows[i].UpdateSnapshot(items[i]);
                }

                return;
            }

            // 3) Si la estructura cambió (por ejemplo, se agotó un ítem y desapareció) → reconstruimos y reseteamos scroll
            BuildRows();
        }

        public void Update()
        {
            LayoutRows();
        }

        private void LayoutRows()
        {
            if (_itemsScroll == null)
            {
                return;
            }

            if (_rows.Count == 0)
            {
                _itemsScroll.SetInnerSize(RowWidth, _itemsScroll.Height);
                _itemsScroll.UpdateScrollBars();
                return;
            }

            var rowWidth = RowWidth;
            var offsetY = 0;

            foreach (var row in _rows)
            {
                row.SetBounds(0, offsetY, rowWidth, row.Height);
                var outerHeight = row.Height + row.Margin.Top + row.Margin.Bottom;
                offsetY += outerHeight;
            }

            _itemsScroll.SetInnerSize(rowWidth, offsetY);
            _itemsScroll.UpdateScrollBars();
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
