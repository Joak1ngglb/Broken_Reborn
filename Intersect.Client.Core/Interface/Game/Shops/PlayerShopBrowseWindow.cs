using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Network.Packets.Localization;
using Intersect.Network.Packets.Shops;

namespace Intersect.Client.Interface.Game.Shops
{
    public sealed class PlayerShopBrowseWindow : Window
    {
        private const int DefaultRowHeight = 68;

        private Label _ownerLabel;
        private ScrollControl _itemsScroll;
        private Label _statusLabel;
        private Label _emptyLabel;
        private readonly List<PlayerShopBrowseItemRow> _rows = new();

        private bool _uiInitialized;
        private bool _hasPendingPurchase;
        private bool _localizationSubscribed;

        private ShopSnapshot _snapshot;

        private int RowOuterHeight =>
            _rows.Count > 0
                ? _rows[0].Height + _rows[0].Margin.Top + _rows[0].Margin.Bottom
                : DefaultRowHeight;

        public PlayerShopBrowseWindow(Canvas parent, ShopSnapshot snapshot)
            : base(parent, Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name), false, nameof(PlayerShopBrowseWindow))
        {
            _snapshot = snapshot;

            // Ventana más compacta
            SetSize(640, 430);
            DisableResizing();

            InitializeUi();
            SubscribeToLocalizationUpdates();
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

            var prevScroll = _itemsScroll.VerticalScrollBar?.ScrollAmount ?? 0f;
            var anchorId = CaptureScrollAnchor(prevScroll);

            var scrollBar = _itemsScroll.VerticalScrollBar;

            // Limpiar filas anteriores
            foreach (var row in _rows)
            {
                row.DetachEvents();
                row.Dispose();
            }
            _rows.Clear();

            var items = _snapshot.Items ?? new List<PlayerShopItemSnapshot>();

            if (items.Count == 0)
            {
                _emptyLabel.IsVisibleInParent = true;

                LayoutRows();
                RestoreScrollPosition(anchorId, prevScroll);

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
            RestoreScrollPosition(anchorId, prevScroll);
            RequestLocalizationEntries();
        }

        public void UpdateSnapshot(ShopSnapshot snapshot)
        {
            _snapshot = snapshot;

            Title = Strings.PlayerShops.BrowserTitle.ToString(snapshot.Name);
            _ownerLabel.Text = Strings.PlayerShops.BrowserOwner.ToString(snapshot.OwnerName);
            RequestLocalizationEntries();

            if (!_uiInitialized || _itemsScroll == null)
            {
                BuildRows();
                return;
            }

            var prevScroll = _itemsScroll.VerticalScrollBar?.ScrollAmount ?? 0f;
            var anchorId = CaptureScrollAnchor(prevScroll);

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
                ResetBuyingState();
                LayoutRows();
                RestoreScrollPosition(anchorId, prevScroll);

                return;
            }

            // 3) Si la estructura cambió (por ejemplo, se agotó un ítem y desapareció) → reconstruimos y reseteamos scroll
            BuildRows();
            ResetBuyingState();
            RestoreScrollPosition(anchorId, prevScroll);
        }

        public void Update()
        {
            LayoutRows();
        }

        private void RequestLocalizationEntries()
        {
            var requests = new List<LocalizationRequestEntry>();
            foreach (var item in _snapshot.Items ?? new List<PlayerShopItemSnapshot>())
            {
                if (!ItemDescriptor.TryGet(item.ItemId, out var descriptor))
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
            if (!IsVisibleInTree || _snapshot.Items == null)
            {
                return;
            }

            var itemType = GameObjectType.Item.ToString();
            if (!requests.Any(
                    request => request.EntityType == itemType &&
                               request.Field == "Name" &&
                               _snapshot.Items.Any(item => item.ItemId.ToString() == request.EntityId)
                ))
            {
                return;
            }

            foreach (var row in _rows)
            {
                row.RefreshLocalization();
            }
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
            if (Globals.Me?.IsDead == true)
            {
                return;
            }

            var quantity = row.RequestedQuantity;
            if (quantity <= 0)
            {
                _statusLabel.Text = Strings.PlayerShops.ErrorInvalidQuantity;
                _statusLabel.SetTextColor(Color.OrangeRed, ComponentState.Normal);
                return;
            }

            PacketSender.SendBuyPlayerShopItem(_snapshot.ShopId, row.Snapshot.ShopItemId, quantity);
            row.SetBuying(true);
            _hasPendingPurchase = true;
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

        private Guid? CaptureScrollAnchor(float scrollAmount)
        {
            if (_itemsScroll?.VerticalScrollBar == null || _rows.Count == 0)
            {
                return null;
            }

            var scrollBar = _itemsScroll.VerticalScrollBar;
            var totalHeight = scrollBar.ContentSize;
            var viewHeight = scrollBar.ViewableContentSize;
            var scrollPixels = (int)(scrollAmount * Math.Max(0, totalHeight - viewHeight));
            var firstIndex = scrollPixels / RowOuterHeight;

            return firstIndex >= 0 && firstIndex < _rows.Count
                ? _rows[firstIndex].Snapshot.ShopItemId
                : null;
        }

        private void RestoreScrollPosition(Guid? anchorId, float prevScroll)
        {
            if (_itemsScroll?.VerticalScrollBar == null)
            {
                return;
            }

            var scrollBar = _itemsScroll.VerticalScrollBar;

            if (anchorId.HasValue)
            {
                for (var i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].Snapshot.ShopItemId != anchorId.Value)
                    {
                        continue;
                    }

                    var newOffset = i * RowOuterHeight;
                    var denominator = Math.Max(0, scrollBar.ContentSize - scrollBar.ViewableContentSize);
                    var amount = denominator > 0 ? (float)newOffset / denominator : 0f;
                    scrollBar.ScrollAmount = Math.Clamp(amount, 0f, 1f);

                    return;
                }
            }

            scrollBar.ScrollAmount = Math.Clamp(prevScroll, 0f, 1f);
        }

        private void ResetBuyingState()
        {
            _hasPendingPurchase = false;
            foreach (var row in _rows)
            {
                row.SetBuying(false);
            }
        }

        internal void NotifyPurchaseFailed(string? message)
        {
            if (!_hasPendingPurchase)
            {
                return;
            }

            ResetBuyingState();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _statusLabel.Text = message;
            _statusLabel.SetTextColor(Color.OrangeRed, ComponentState.Normal);
        }
    }
}
