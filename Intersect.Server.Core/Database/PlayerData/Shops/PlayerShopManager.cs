using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Server.Database;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Intersect.Server.Database.PlayerData.Shops;

public static class PlayerShopManager
{
    private static readonly ConcurrentDictionary<Guid, PlayerShopRuntime> ActiveShops = new();

    public static IReadOnlyCollection<PlayerShopRuntime> GetActiveShops()
        => ActiveShops.Values.ToList().AsReadOnly();

    public static bool TryGetShop(Guid shopId, out PlayerShopRuntime runtime)
        => ActiveShops.TryGetValue(shopId, out runtime);

    public static void LoadActiveShops()
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: true, explicitLoad: true);
        var shops = context.Player_Shops
            .Include(shop => shop.Items)
            .AsNoTracking()
            .Where(shop => shop.Status == PlayerShopStatus.Active)
            .ToList();

        ActiveShops.Clear();

        foreach (var shop in shops)
        {
            ActiveShops[shop.Id] = new PlayerShopRuntime(shop);
        }

        Log.Information("Loaded {Count} active player shops", ActiveShops.Count);
    }

    public static PlayerShopRuntime CreateShop(
        Player owner,
        Guid mapId,
        int x,
        int y,
        int z,
        IEnumerable<PlayerShopStock> stock,
        DateTime? expiresAt = null,
        string? title = null
    )
    {
        ArgumentNullException.ThrowIfNull(owner);

        var shop = new PlayerShop
        {
            OwnerId = owner.Id,
            MapId = mapId,
            X = x,
            Y = y,
            Z = z,
            Title = title ?? owner.Name,
            Status = PlayerShopStatus.Active,
            ExpiresAt = expiresAt,
        };

        foreach (var entry in stock ?? Enumerable.Empty<PlayerShopStock>())
        {
            shop.Items.Add(entry.ToEntity());
        }

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        context.Player_Shops.Add(shop);
        context.SaveChanges();

        var runtime = new PlayerShopRuntime(shop);
        ActiveShops[shop.Id] = runtime;

        return runtime;
    }

    public static bool UpdateStock(Guid shopId, IEnumerable<PlayerShopStock> stock)
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var shop = context.Player_Shops
            .Include(s => s.Items)
            .FirstOrDefault(s => s.Id == shopId && s.Status == PlayerShopStatus.Active);

        if (shop == null)
        {
            return false;
        }

        context.Player_ShopItems.RemoveRange(shop.Items);
        shop.Items.Clear();

        foreach (var entry in stock ?? Enumerable.Empty<PlayerShopStock>())
        {
            shop.Items.Add(entry.ToEntity());
        }

        context.SaveChanges();

        ActiveShops.AddOrUpdate(
            shopId,
            _ => new PlayerShopRuntime(shop),
            (_, existing) =>
            {
                existing.ReplaceItems(shop.Items);
                return existing;
            }
        );

        return true;
    }

    public static bool LogTransaction(Guid shopId, Guid shopItemId, string buyerName, int quantity, int unitPrice)
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var shop = context.Player_Shops
            .Include(s => s.Items)
            .FirstOrDefault(s => s.Id == shopId);

        if (shop == null)
        {
            return false;
        }

        var item = shop.Items.FirstOrDefault(i => i.Id == shopItemId);
        if (item == null)
        {
            return false;
        }

        var totalPrice = checked(quantity * unitPrice);
        item.IsSold = true;
        item.SoldAt = DateTime.UtcNow;

        var transaction = new PlayerShopTransaction
        {
            ShopId = shop.Id,
            ShopItemId = item.Id,
            OwnerId = shop.OwnerId,
            ItemId = item.ItemId,
            BuyerName = buyerName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            ItemProperties = new ItemProperties(item.Properties),
        };

        shop.PendingGold += totalPrice;
        context.Player_ShopTransactions.Add(transaction);
        context.SaveChanges();

        if (ActiveShops.TryGetValue(shopId, out var runtime))
        {
            lock (runtime.SyncRoot)
            {
                if (runtime.TryGetItem(shopItemId, out var runtimeItem))
                {
                    runtimeItem.MarkSold();
                }

                runtime.UpdatePendingGold(shop.PendingGold);
            }
        }

        return true;
    }

    public static bool TryPayout(Guid shopId, out long pendingGold)
    {
        pendingGold = 0;

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var shop = context.Player_Shops.FirstOrDefault(s => s.Id == shopId);
        if (shop == null)
        {
            return false;
        }

        if (shop.PendingGold <= 0)
        {
            return false;
        }

        pendingGold = shop.PendingGold;
        shop.PendingGold = 0;
        context.SaveChanges();

        if (ActiveShops.TryGetValue(shopId, out var runtime))
        {
            runtime.UpdatePendingGold(0);
        }

        return true;
    }

    public static bool CloseShop(Guid shopId, PlayerShopStatus status = PlayerShopStatus.Closed)
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var shop = context.Player_Shops.FirstOrDefault(s => s.Id == shopId);
        if (shop == null)
        {
            return false;
        }

        shop.Status = status;
        shop.ClosedAt = DateTime.UtcNow;
        context.SaveChanges();

        if (ActiveShops.TryRemove(shopId, out var runtime))
        {
            runtime.UpdateStatus(status);
        }

        return true;
    }

    public static int CloseExpiredShops()
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var now = DateTime.UtcNow;
        var expired = context.Player_Shops
            .Where(shop => shop.Status == PlayerShopStatus.Active && shop.ExpiresAt != null && shop.ExpiresAt <= now)
            .ToList();

        if (expired.Count == 0)
        {
            return 0;
        }

        foreach (var shop in expired)
        {
            shop.Status = PlayerShopStatus.Expired;
            shop.ClosedAt = now;
            ActiveShops.TryRemove(shop.Id, out _);
        }

        context.SaveChanges();
        return expired.Count;
    }

    public readonly record struct PlayerShopStock(Guid ItemId, int Quantity, int PricePerUnit, ItemProperties? Properties)
    {
        public PlayerShopItem ToEntity()
        {
            return new PlayerShopItem
            {
                ItemId = ItemId,
                Quantity = Quantity,
                PricePerUnit = PricePerUnit,
                Properties = Properties != null ? new ItemProperties(Properties) : new ItemProperties(),
            };
        }
    }

    public sealed class PlayerShopRuntime
    {
        private readonly Dictionary<Guid, PlayerShopItemRuntime> _items;

        internal PlayerShopRuntime(PlayerShop shop)
        {
            ShopId = shop.Id;
            OwnerId = shop.OwnerId;
            MapId = shop.MapId;
            X = shop.X;
            Y = shop.Y;
            Z = shop.Z;
            Title = shop.Title;
            Status = shop.Status;
            PendingGold = shop.PendingGold;
            CreatedAt = shop.CreatedAt;
            ExpiresAt = shop.ExpiresAt;
            _items = shop.Items?.ToDictionary(item => item.Id, item => new PlayerShopItemRuntime(item))
                ?? new Dictionary<Guid, PlayerShopItemRuntime>();
        }

        internal object SyncRoot { get; } = new();

        public Guid ShopId { get; }

        public Guid OwnerId { get; }

        public Guid MapId { get; }

        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        public string Title { get; }

        public PlayerShopStatus Status { get; private set; }

        public long PendingGold { get; private set; }

        public DateTime CreatedAt { get; }

        public DateTime? ExpiresAt { get; }

        public IReadOnlyCollection<PlayerShopItemRuntime> Items => _items.Values;

        internal void ReplaceItems(IEnumerable<PlayerShopItem> items)
        {
            lock (SyncRoot)
            {
                _items.Clear();
                foreach (var item in items)
                {
                    _items[item.Id] = new PlayerShopItemRuntime(item);
                }
            }
        }

        internal bool TryGetItem(Guid itemId, out PlayerShopItemRuntime runtime) => _items.TryGetValue(itemId, out runtime);

        internal void UpdatePendingGold(long pendingGold)
        {
            lock (SyncRoot)
            {
                PendingGold = pendingGold;
            }
        }

        internal void UpdateStatus(PlayerShopStatus status)
        {
            lock (SyncRoot)
            {
                Status = status;
            }
        }
    }

    public sealed class PlayerShopItemRuntime
    {
        internal PlayerShopItemRuntime(PlayerShopItem item)
        {
            Id = item.Id;
            Item = new Item(item.ItemId, item.Quantity)
            {
                Properties = new ItemProperties(item.Properties)
            };
            Quantity = item.Quantity;
            PricePerUnit = item.PricePerUnit;
            IsSold = item.IsSold;
        }

        public Guid Id { get; }

        public Item Item { get; }

        public int Quantity { get; }

        public int PricePerUnit { get; }

        public bool IsSold { get; private set; }

        internal void MarkSold()
        {
            IsSold = true;
        }

        public Item CloneItem() => new(Item);
    }
}
