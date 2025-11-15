using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Network.Packets.Shops;
using Intersect.Server.Database;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Entities;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Intersect.Server.Database.PlayerData.Shops;

public static class PlayerShopManager
{
    private static readonly ConcurrentDictionary<Guid, PlayerShopRuntime> ActiveShops = new();
    private static readonly ConcurrentDictionary<Guid, PlayerShopEntity> ActiveShopEntities = new();

    internal static void SpawnShopsForInstance(MapInstance mapInstance)
    {
        if (mapInstance == null)
        {
            return;
        }

        foreach (var runtime in ActiveShops.Values)
        {
            if (runtime.MapId != mapInstance.MapId)
            {
                continue;
            }

            var shouldSpawn = runtime.MapInstanceId == mapInstance.MapInstanceId;

            if (!shouldSpawn && mapInstance.MapInstanceId == MapInstance.OverworldInstanceId)
            {
                if (!MapController.TryGetInstanceFromMap(runtime.MapId, runtime.MapInstanceId, out _))
                {
                    runtime.UpdateMapInstanceId(MapInstance.OverworldInstanceId);
                    shouldSpawn = true;
                }
            }

            if (!shouldSpawn)
            {
                continue;
            }

            EnsureShopEntity(runtime, mapInstance);
        }
    }

    private static void EnsureShopEntity(PlayerShopRuntime runtime, MapInstance mapInstance)
    {
        if (runtime == null || mapInstance == null)
        {
            return;
        }

        if (ActiveShopEntities.TryGetValue(runtime.ShopId, out var existing))
        {
            if (existing.MapInstanceId == mapInstance.MapInstanceId && mapInstance.ContainsEntity(existing.Id))
            {
                return;
            }

            DespawnShopEntity(runtime.ShopId, existing);
        }

        var entity = new PlayerShopEntity(runtime, mapInstance.MapInstanceId);
        mapInstance.AddEntity(entity);
        runtime.UpdateMapInstanceId(mapInstance.MapInstanceId);
        ActiveShopEntities[runtime.ShopId] = entity;

        PacketSender.SendEntityDataToProximity(entity);
        PacketSender.SendEntityPositionToAll(entity);
    }

    private static void TrySpawnShopEntity(PlayerShopRuntime runtime, Guid? mapInstanceIdOverride = null)
    {
        if (runtime == null)
        {
            return;
        }

        var targetInstanceId = mapInstanceIdOverride ?? runtime.MapInstanceId;

        if (!MapController.TryGetInstanceFromMap(runtime.MapId, targetInstanceId, out var mapInstance))
        {
            if (targetInstanceId != MapInstance.OverworldInstanceId &&
                MapController.TryGetInstanceFromMap(runtime.MapId, MapInstance.OverworldInstanceId, out mapInstance))
            {
                runtime.UpdateMapInstanceId(MapInstance.OverworldInstanceId);
            }
            else
            {
                return;
            }
        }

        EnsureShopEntity(runtime, mapInstance);
    }

    private static void DespawnAllShopEntities()
    {
        foreach (var shopId in ActiveShopEntities.Keys.ToArray())
        {
            DespawnShopEntity(shopId);
        }
    }

    private static void DespawnShopEntity(Guid shopId, PlayerShopEntity? entity = null)
    {
        entity ??= ActiveShopEntities.TryGetValue(shopId, out var existing) ? existing : null;

        if (entity == null)
        {
            return;
        }

        ActiveShopEntities.TryRemove(shopId, out _);

        if (MapController.TryGetInstanceFromMap(entity.MapId, entity.MapInstanceId, out var mapInstance))
        {
            mapInstance.RemoveEntity(entity);
        }

        PacketSender.SendEntityLeave(entity);
    }

    private static void UpdatePlayerActiveShop(Guid ownerId, Guid? shopId, PlayerShopStatus? status, Player? owner = null)
    {
        if (ownerId == Guid.Empty)
        {
            return;
        }

        owner ??= Player.FindOnline(ownerId);
        if (owner != null)
        {
            owner.ActivePlayerShopId = shopId;
            owner.ActivePlayerShopStatus = status;
        }

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        context.Database.ExecuteSqlInterpolated(
            $"UPDATE Players SET ActivePlayerShopId = {shopId}, ActivePlayerShopStatus = {status} WHERE Id = {ownerId}"
        );
    }

    public static IReadOnlyCollection<PlayerShopRuntime> GetActiveShops()
        => ActiveShops.Values.ToList().AsReadOnly();

    public static bool TryGetShop(Guid shopId, out PlayerShopRuntime runtime)
        => ActiveShops.TryGetValue(shopId, out runtime);

    public static void LoadActiveShops()
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: true, explicitLoad: true);
        var shops = context.Player_Shops
            .Include(shop => shop.Owner)
            .Include(shop => shop.Items)
            .AsNoTracking()
            .Where(shop => shop.Status == PlayerShopStatus.Active)
            .ToList();

        DespawnAllShopEntities();
        ActiveShops.Clear();

        foreach (var shop in shops)
        {
            var runtime = new PlayerShopRuntime(shop);
            ActiveShops[shop.Id] = runtime;
            UpdatePlayerActiveShop(shop.OwnerId, shop.Id, PlayerShopStatus.Active);
            TrySpawnShopEntity(runtime);
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

        var ownerName = owner?.Name ?? string.Empty;

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

        var runtime = new PlayerShopRuntime(shop, ownerName, owner.MapInstanceId);
        ActiveShops[shop.Id] = runtime;

        UpdatePlayerActiveShop(owner.Id, shop.Id, PlayerShopStatus.Active, owner);
        TrySpawnShopEntity(runtime, owner.MapInstanceId);

        return runtime;
    }

    public static bool UpdateStock(Guid shopId, IEnumerable<PlayerShopStock> stock)
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var shop = context.Player_Shops
            .Include(s => s.Owner)
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

    public static bool TryCommitPurchase(
        Guid shopId,
        Guid shopItemId,
        string buyerName,
        int quantity,
        out long pendingGold
    )
    {
        pendingGold = 0;

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        var item = context.Player_ShopItems
            .Include(i => i.Shop)
            .FirstOrDefault(i => i.ShopId == shopId && i.Id == shopItemId);

        if (item?.Shop == null)
        {
            transaction.Rollback();
            return false;
        }

        if (item.IsSold || item.Quantity < quantity)
        {
            transaction.Rollback();
            return false;
        }

        var totalPrice = checked(quantity * item.PricePerUnit);

        item.Quantity -= quantity;
        if (item.Quantity <= 0)
        {
            item.Quantity = 0;
            item.IsSold = true;
            item.SoldAt = DateTime.UtcNow;
        }

        var logEntry = new PlayerShopTransaction
        {
            ShopId = item.ShopId,
            ShopItemId = item.Id,
            OwnerId = item.Shop.OwnerId,
            ItemId = item.ItemId,
            BuyerName = buyerName,
            Quantity = quantity,
            UnitPrice = item.PricePerUnit,
            TotalPrice = totalPrice,
            ItemProperties = new ItemProperties(item.Properties),
        };

        var rowsAffected = context.Database.ExecuteSqlInterpolated(
            $"UPDATE Player_Shops SET PendingGold = PendingGold + {totalPrice} WHERE Id = {item.ShopId}"
        );

        if (rowsAffected != 1)
        {
            transaction.Rollback();
            return false;
        }

        context.Player_ShopTransactions.Add(logEntry);
        context.SaveChanges();
        transaction.Commit();

        pendingGold = context.Player_Shops
            .AsNoTracking()
            .Where(s => s.Id == item.ShopId)
            .Select(s => (long?)s.PendingGold)
            .FirstOrDefault() ?? 0;

        return true;
    }

    public static bool TryPayout(Guid shopId, out long pendingGold)
    {
        pendingGold = 0;

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        const int maxAttempts = 5;
        var encounteredBalance = false;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var balance = context.Player_Shops
                .AsNoTracking()
                .Where(s => s.Id == shopId)
                .Select(s => new { s.PendingGold })
                .FirstOrDefault();

            if (balance == null)
            {
                return false;
            }

            if (balance.PendingGold <= 0)
            {
                return false;
            }

            encounteredBalance = true;

            var rows = context.Database.ExecuteSqlInterpolated(
                $"UPDATE Player_Shops SET PendingGold = PendingGold - {balance.PendingGold} WHERE Id = {shopId} AND PendingGold = {balance.PendingGold}"
            );

            if (rows == 0)
            {
                continue;
            }

            pendingGold = balance.PendingGold;
            break;
        }

        if (pendingGold <= 0)
        {
            if (encounteredBalance)
            {
                Log.Warning(
                    "Failed to payout player shop {ShopId} after {Attempts} attempts due to concurrent updates.",
                    shopId,
                    maxAttempts
                );
            }

            return false;
        }

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

        UpdatePlayerActiveShop(runtime?.OwnerId ?? shop.OwnerId, null, status);
        DespawnShopEntity(shopId);

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

        var owners = new List<Guid>(expired.Count);
        foreach (var shop in expired)
        {
            shop.Status = PlayerShopStatus.Expired;
            shop.ClosedAt = now;
            owners.Add(shop.OwnerId);
            ActiveShops.TryRemove(shop.Id, out _);
            DespawnShopEntity(shop.Id);
        }

        context.SaveChanges();

        foreach (var ownerId in owners)
        {
            UpdatePlayerActiveShop(ownerId, null, PlayerShopStatus.Expired);
        }

        return expired.Count;
    }

    public static bool TryBuildSnapshot(Guid shopId, out ShopSnapshot snapshot)
    {
        if (!ActiveShops.TryGetValue(shopId, out var runtime))
        {
            snapshot = default;
            return false;
        }

        snapshot = BuildSnapshot(runtime);
        return true;
    }

    public static ShopSnapshot BuildSnapshot(PlayerShopRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var snapshot = new ShopSnapshot
        {
            ShopId = runtime.ShopId,
            Name = runtime.Title,
            OwnerName = runtime.OwnerName,
        };

        foreach (var item in runtime.Items)
        {
            if (item.IsSold || item.Quantity <= 0)
            {
                continue;
            }

            snapshot.Items.Add(
                new PlayerShopItemSnapshot
                {
                    ShopItemId = item.Id,
                    ItemId = item.Item.ItemId,
                    Quantity = item.Quantity,
                    PricePerUnit = item.PricePerUnit,
                    Properties = new ItemProperties(item.Item.Properties),
                }
            );
        }

        return snapshot;
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

    public sealed record PlayerShopFinalizationSummary(
        Guid ShopId,
        string ShopName,
        long GoldPaid,
        int ReturnedStackCount,
        int ReturnedItemQuantity,
        ShopSnapshot Snapshot
    );

    public static bool TryFinalizeActiveShop(Player player, out PlayerShopFinalizationSummary? summary)
    {
        summary = null;

        if (player == null)
        {
            return false;
        }

        if (!player.ActivePlayerShopId.HasValue || player.ActivePlayerShopStatus != PlayerShopStatus.Active)
        {
            return false;
        }

        var shopId = player.ActivePlayerShopId.Value;

        if (!ActiveShops.TryGetValue(shopId, out var runtime))
        {
            using var context = DbInterface.CreatePlayerContext(readOnly: true, explicitLoad: true);
            var entity = context.Player_Shops
                .Include(shop => shop.Items)
                .Include(shop => shop.Owner)
                .FirstOrDefault(shop => shop.Id == shopId && shop.Status == PlayerShopStatus.Active);

            if (entity == null)
            {
                return false;
            }

            runtime = new PlayerShopRuntime(entity, entity.Owner?.Name);
        }

        lock (runtime.SyncRoot)
        {
            var snapshot = BuildSnapshot(runtime);
            var unsoldItems = runtime.Items
                .Where(item => !item.IsSold && item.Quantity > 0)
                .Select(item => item.CloneItem())
                .ToList();

            var returnedStackCount = unsoldItems.Count;
            var returnedItemQuantity = unsoldItems.Sum(item => item.Quantity);

            long goldPaid = 0;
            if (runtime.PendingGold > 0)
            {
                var currencyDescriptor = ResolveGlobalCurrencyDescriptor();
                if (currencyDescriptor == null)
                {
                    Log.Warning(
                        "Cannot finalize player shop {ShopId} for {PlayerId} because no global currency exists",
                        runtime.ShopId,
                        player.Id
                    );

                    return false;
                }

                if (!TryPayout(runtime.ShopId, out var pendingGold) || pendingGold <= 0)
                {
                    Log.Warning(
                        "Failed to payout pending gold for shop {ShopId} owned by {OwnerId}",
                        runtime.ShopId,
                        runtime.OwnerId
                    );

                    return false;
                }

                goldPaid = pendingGold;
                runtime.UpdatePendingGold(0);
                DeliverCurrency(player, runtime.ShopId, currencyDescriptor.Id, goldPaid);
            }

            if (unsoldItems.Count > 0)
            {
                DeliverItems(player, runtime.ShopId, unsoldItems);
            }

            CloseShop(runtime.ShopId, PlayerShopStatus.Closed);
            player.ActivePlayerShopId = null;
            player.ActivePlayerShopStatus = null;

            summary = new PlayerShopFinalizationSummary(
                runtime.ShopId,
                runtime.Title,
                goldPaid,
                returnedStackCount,
                returnedItemQuantity,
                snapshot
            );

            return true;
        }
    }

    public sealed class PlayerShopRuntime
    {
        private readonly Dictionary<Guid, PlayerShopItemRuntime> _items;

        internal PlayerShopRuntime(PlayerShop shop, string? ownerName = null, Guid? mapInstanceId = null)
        {
            ShopId = shop.Id;
            OwnerId = shop.OwnerId;
            MapId = shop.MapId;
            X = shop.X;
            Y = shop.Y;
            Z = shop.Z;
            Title = shop.Title;
            OwnerName = ownerName ?? shop.Owner?.Name ?? string.Empty;
            Status = shop.Status;
            PendingGold = shop.PendingGold;
            CreatedAt = shop.CreatedAt;
            ExpiresAt = shop.ExpiresAt;
            MapInstanceId = mapInstanceId ?? MapInstance.OverworldInstanceId;
            _items = shop.Items?.ToDictionary(item => item.Id, item => new PlayerShopItemRuntime(item))
                ?? new Dictionary<Guid, PlayerShopItemRuntime>();
        }

        internal object SyncRoot { get; } = new();

        public Guid ShopId { get; }

        public Guid OwnerId { get; }

        public string OwnerName { get; }

        public Guid MapId { get; }

        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        public string Title { get; }

        public PlayerShopStatus Status { get; private set; }

        public long PendingGold { get; private set; }

        public DateTime CreatedAt { get; }

        public DateTime? ExpiresAt { get; }

        public Guid MapInstanceId { get; private set; }

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

        internal void UpdateMapInstanceId(Guid mapInstanceId)
        {
            lock (SyncRoot)
            {
                MapInstanceId = mapInstanceId;
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

        public int Quantity { get; private set; }

        public int PricePerUnit { get; }

        public bool IsSold { get; private set; }

        internal void ReduceQuantity(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Quantity = Math.Max(0, Quantity - amount);
            if (Quantity == 0)
            {
                MarkSold();
            }
        }

        internal void MarkSold()
        {
            IsSold = true;
            Quantity = 0;
        }

        public Item CloneItem() => new(Item);

        public Item CloneItem(int quantity)
        {
            var clone = new Item(Item);
            clone.Quantity = quantity;
            return clone;
        }
    }

    internal static ItemDescriptor? ResolveGlobalCurrencyDescriptor()
    {
        return ItemDescriptor.Lookup.Values
            .OfType<ItemDescriptor>()
            .FirstOrDefault(descriptor => descriptor.ItemType == ItemType.Currency);
    }

    private static void DeliverCurrency(Player player, Guid shopId, Guid currencyItemId, long amount)
    {
        if (player == null || amount <= 0)
        {
            return;
        }

        var remaining = amount;
        var stacks = new List<Item>();

        while (remaining > 0)
        {
            var chunk = (int)Math.Min(remaining, int.MaxValue);
            stacks.Add(new Item(currencyItemId, chunk));
            remaining -= chunk;
        }

        DeliverItems(player, shopId, stacks);
    }

    private static void DeliverItems(Player player, Guid shopId, IEnumerable<Item> items)
    {
        if (player == null)
        {
            return;
        }

        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (player.TryGiveItem(item, ItemHandling.Normal, bankOverflow: true, slot: -1, sendUpdate: false))
            {
                continue;
            }

            Log.Warning(
                "Failed to deliver item {ItemId} x{Quantity} to player {PlayerId} while closing shop {ShopId}",
                item.ItemId,
                item.Quantity,
                player.Id,
                shopId
            );
        }
    }
}
