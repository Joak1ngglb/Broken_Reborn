using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Network.Packets.Shops;
using Intersect.Server.Database;
using Intersect.Server.Database.PlayerData;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Entities;
using Intersect.Server.Maps;
using Intersect.Server.Framework.Items;
using Intersect.Server.Networking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Serilog;

namespace Intersect.Server.Database.PlayerData.Shops;

public static class PlayerShopManager
{
    private static readonly ConcurrentDictionary<Guid, PlayerShopRuntime> ActiveShops = new();
    private static readonly ConcurrentDictionary<Guid, PlayerShopEntity> ActiveShopEntities = new();

    private static PlayerShopRuntime ActivateRuntime(
        PlayerShopRuntime runtime,
        Player? owner = null,
        bool spawnEntity = true
    )
    {
        if (runtime == null)
        {
            return runtime!;
        }

        if (spawnEntity)
        {
            TrySpawnShopEntity(runtime);
        }

        UpdatePlayerActiveShop(runtime.OwnerId, runtime.ShopId, PlayerShopStatus.Active, owner);

        return runtime;
    }

    private static void RegisterActiveShop(
        PlayerShopRuntime runtime,
        Player? owner = null,
        bool spawnEntity = true
    )
    {
        if (runtime == null)
        {
            return;
        }

        ActiveShops[runtime.ShopId] = runtime;
        ActivateRuntime(runtime, owner, spawnEntity);
    }

    private static bool IsLocationOccupied(Guid mapId, Guid mapInstanceId, int x, int y, int z)
    {
        return ActiveShops.Values.Any(
            shop =>
                shop.MapId == mapId
                && shop.MapInstanceId == mapInstanceId
                && shop.X == x
                && shop.Y == y
                && shop.Z == z
                && shop.Status == PlayerShopStatus.Active
        );
    }

    private static bool TrySpawnShopEntity(PlayerShopRuntime runtime, MapInstance? mapInstance = null)
    {
        if (runtime == null)
        {
            return false;
        }

        if (ActiveShopEntities.ContainsKey(runtime.ShopId))
        {
            return true;
        }

        mapInstance ??= MapController.TryGetInstanceFromMap(runtime.MapId, runtime.MapInstanceId, out var resolvedInstance)
            ? resolvedInstance
            : null;

        if (mapInstance == null)
        {
            return false;
        }

        var entity = new PlayerShopEntity(runtime, mapInstance.MapInstanceId);
        if (!ActiveShopEntities.TryAdd(runtime.ShopId, entity))
        {
            return false;
        }

        mapInstance.AddEntity(entity);
        PacketSender.SendEntityDataToProximity(entity);

        return true;
    }

    internal static void SpawnShopsForInstance(MapInstance mapInstance)
    {
        if (mapInstance == null)
        {
            return;
        }

        foreach (
            var runtime in ActiveShops.Values.Where(
                shop => shop.MapId == mapInstance.MapId
                    && (shop.MapInstanceId == mapInstance.MapInstanceId || shop.MapInstanceId == Guid.Empty)
            )
        )
        {
            TrySpawnShopEntity(runtime, mapInstance);
        }
    }

    internal static void DespawnShopsForInstance(MapInstance mapInstance)
    {
        if (mapInstance == null)
        {
            return;
        }

        foreach (
            var shopId in ActiveShopEntities
                .Where(pair => pair.Value.MapInstanceId == mapInstance.MapInstanceId)
                .Select(pair => pair.Key)
                .ToList()
        )
        {
            DespawnShopEntity(shopId);
        }
    }

    private static void DespawnShopEntity(Guid shopId)
    {
        if (!ActiveShopEntities.TryRemove(shopId, out var entity) || entity == null)
        {
            return;
        }

        if (MapController.TryGetInstanceFromMap(entity.MapId, entity.MapInstanceId, out var mapInstance))
        {
            mapInstance.RemoveEntity(entity);
        }

        PacketSender.SendEntityLeave(entity);
    }

    private static void UpdatePlayerActiveShop(
        Guid ownerId,
        Guid? shopId,
        PlayerShopStatus? status,
        Player? owner = null,
        PlayerContext? context = null,
        bool saveChanges = true
    )
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

        if (context == null)
        {
            using var ownedContext = DbInterface.CreatePlayerContext(readOnly: false);
            UpdatePlayerActiveShop(ownerId, shopId, status, context: ownedContext);
            return;
        }

        var persistedOwner = context.Players.FirstOrDefault(player => player.Id == ownerId);
        if (persistedOwner == null)
        {
            return;
        }

        persistedOwner.ActivePlayerShopId = shopId;
        persistedOwner.ActivePlayerShopStatus = status;

        if (saveChanges)
        {
            context.SaveChanges();
        }
    }

    public static IReadOnlyCollection<PlayerShopRuntime> GetActiveShops()
        => ActiveShops.Values.ToList().AsReadOnly();

    public static bool TryGetShop(Guid shopId, out PlayerShopRuntime runtime)
        => ActiveShops.TryGetValue(shopId, out runtime);

    public static bool TryGetShopStatus(Guid shopId, out PlayerShopStatus status)
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: true);
        var resolvedStatus = context.Player_Shops
            .AsNoTracking()
            .Where(shop => shop.Id == shopId)
            .Select(shop => (PlayerShopStatus?)shop.Status)
            .FirstOrDefault();

        if (!resolvedStatus.HasValue)
        {
            status = default;
            return false;
        }

        status = resolvedStatus.Value;
        return true;
    }

    public static void LoadActiveShops()
    {
        using var context = DbInterface.CreatePlayerContext(readOnly: true, explicitLoad: true);
        var shops = context.Player_Shops
            .Include(shop => shop.Owner)
            .Include(shop => shop.Items)
            .AsNoTracking()
            .Where(shop => shop.Status == PlayerShopStatus.Active)
            .ToList();

        ActiveShops.Clear();
        foreach (var shopId in ActiveShopEntities.Keys.ToList())
        {
            DespawnShopEntity(shopId);
        }

        foreach (var shop in shops)
        {
            var runtime = new PlayerShopRuntime(shop, shop.Owner?.Name, shop.Owner);
            RegisterActiveShop(runtime, spawnEntity: false);
        }

        Log.Information("Loaded {Count} active player shops", ActiveShops.Count);
    }

    public static void RespawnActiveShopEntities()
    {
        foreach (var runtime in ActiveShops.Values)
        {
            TrySpawnShopEntity(runtime);
        }
    }

    public static PlayerShopRuntime CreateShop(
        Player owner,
        Guid mapId,
        int x,
        int y,
        int z,
        IEnumerable<PlayerShopStock> stock,
        DateTime? expiresAt = null,
        string? title = null,
        string? decoration = null
    )
    {
        ArgumentNullException.ThrowIfNull(owner);

        var ownerName = owner?.Name ?? string.Empty;
        var stockEntries = (stock ?? Enumerable.Empty<PlayerShopStock>()).ToList();

        if (stockEntries.Count == 0)
        {
            throw new InvalidOperationException("La tienda no tiene artículos configurados.");
        }

        if (owner.MapInstanceId != MapInstance.OverworldInstanceId)
        {
            throw new InvalidOperationException("Player shops can only be created on non-instanced maps.");
        }

        if (IsLocationOccupied(mapId, owner.MapInstanceId, x, y, z))
        {
            throw new InvalidOperationException("Ya existe una tienda en esta ubicación.");
        }

        IReadOnlyList<ReservedInventoryItem> reservations = Array.Empty<ReservedInventoryItem>();
        var persistenceCommitted = false;

        try
        {
            // Fase 1: reservar artículos únicamente en memoria.
            reservations = ReserveInventory(owner, stockEntries);

            if (reservations.Count > 0)
            {
                owner.Save();
            }

            // Fase 2: persistencia atómica de tienda/items + estado activo del owner.
            PlayerShop shop;
            using (var context = DbInterface.CreatePlayerContext(readOnly: false))
            using (var transaction = context.Database.BeginTransaction())
            {
                shop = new PlayerShop
                {
                    OwnerId = owner.Id,
                    MapId = mapId,
                    MapInstanceId = owner.MapInstanceId,
                    X = x,
                    Y = y,
                    Z = z,
                    Title = title ?? owner.Name,
                    Status = PlayerShopStatus.Active,
                    ExpiresAt = expiresAt,
                    Decoration = string.IsNullOrWhiteSpace(decoration)
                        ? PlayerShopEntityConstants.DefaultDecoration
                        : decoration,
                };

                foreach (var entry in stockEntries)
                {
                    shop.Items.Add(entry.ToEntity());
                }

                context.Player_Shops.Add(shop);
                UpdatePlayerActiveShop(
                    owner.Id,
                    shop.Id,
                    PlayerShopStatus.Active,
                    owner,
                    context,
                    saveChanges: false
                );
                context.SaveChanges();

                transaction.Commit();
                persistenceCommitted = true;
            }

            var runtime = new PlayerShopRuntime(shop, ownerName, owner);
            RegisterActiveShop(runtime, owner);

            return runtime;
        }
        catch (Exception exception)
        {
            if (!persistenceCommitted && reservations.Count > 0)
            {
                RestoreInventory(owner, reservations);
            }
            else if (persistenceCommitted)
            {
                Log.Error(
                    exception,
                    "Player shop persistence already committed for owner {OwnerId} at map {MapId} ({X}, {Y}, {Z}). " +
                    "Automatic inventory rollback is disabled post-commit; run explicit compensation workflow.",
                    owner.Id,
                    mapId,
                    x,
                    y,
                    z
                );
            }

            if (IsActiveShopUniquenessViolation(exception))
            {
                throw new InvalidOperationException(
                    "Ya tienes una tienda activa o la ubicación ya está ocupada por otra tienda activa.",
                    exception
                );
            }

            throw;
        }
    }

    private static bool IsActiveShopUniquenessViolation(Exception exception)
    {
        if (exception is not DbUpdateException dbUpdateException)
        {
            return false;
        }

        if (dbUpdateException.InnerException is SqliteException sqliteException)
        {
            return sqliteException.SqliteErrorCode == 19;
        }

        if (dbUpdateException.InnerException is MySqlException mySqlException)
        {
            return mySqlException.Number == 1062;
        }

        var message = dbUpdateException.InnerException?.Message ?? dbUpdateException.Message;
        return message.Contains("unique", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || message.Contains("constraint", StringComparison.OrdinalIgnoreCase);
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
            _ => ActivateRuntime(new PlayerShopRuntime(shop, shop.Owner?.Name, shop.Owner), shop.Owner),
            (_, runtime) =>
            {
                runtime.ReplaceItems(shop.Items);
                return runtime;
            }
        );

        return true;
    }

    private static IReadOnlyList<ReservedInventoryItem> ReserveInventory(
        Player owner,
        IReadOnlyCollection<PlayerShopStock> stockEntries
    )
    {
        if (stockEntries == null || stockEntries.Count == 0)
        {
            return Array.Empty<ReservedInventoryItem>();
        }

        var reservations = new Dictionary<int, SlotReservation>();

        foreach (var entry in stockEntries)
        {
            if (!entry.InventorySlot.HasValue)
            {
                throw new InvalidOperationException("Falta el espacio de inventario para uno de los artículos.");
            }

            if (entry.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    $"La cantidad para el espacio {entry.InventorySlot.Value + 1} debe ser mayor a 0."
                );
            }

            var slotIndex = entry.InventorySlot.Value;

            if (slotIndex < 0 || slotIndex >= owner.Items.Count)
            {
                throw new InvalidOperationException("El espacio seleccionado no es válido.");
            }

            if (!owner.TryGetSlot(slotIndex, out var slot))
            {
                throw new InvalidOperationException("El espacio seleccionado no es válido.");
            }

            if (slot == null || slot.ItemId == Guid.Empty)
            {
                throw new InvalidOperationException($"El espacio {slotIndex + 1} está vacío.");
            }

            if (slot.ItemId != entry.ItemId)
            {
                throw new InvalidOperationException(
                    $"El artículo en el espacio {slotIndex + 1} no coincide con el listado."
                );
            }

            if (!reservations.TryGetValue(slotIndex, out var slotReservation))
            {
                var propertiesClone = entry.Properties != null
                    ? new ItemProperties(entry.Properties)
                    : new ItemProperties(slot.Properties);

                slotReservation = new SlotReservation(slotIndex, slot, slot.ItemId, propertiesClone);
                reservations[slotIndex] = slotReservation;
            }

            slotReservation.QuantityToTake += entry.Quantity;

            if (slotReservation.QuantityToTake > slot.Quantity)
            {
                throw new InvalidOperationException($"No tienes suficientes unidades en el espacio {slotIndex + 1}.");
            }
        }

        if (reservations.Count == 0)
        {
            return Array.Empty<ReservedInventoryItem>();
        }

        var removed = new List<ReservedInventoryItem>(reservations.Count);

        foreach (var reservation in reservations.Values)
        {
            if (!owner.TryTakeItem(reservation.Slot, reservation.QuantityToTake))
            {
                throw new InvalidOperationException(
                    $"No se pudieron reservar los artículos del espacio {reservation.SlotIndex + 1}."
                );
            }

            removed.Add(
                new ReservedInventoryItem(
                    reservation.SlotIndex,
                    reservation.ItemId,
                    reservation.QuantityToTake,
                    new ItemProperties(reservation.Properties)
                )
            );
        }

        return removed;
    }

    private static void RestoreInventory(Player owner, IReadOnlyList<ReservedInventoryItem> reservations)
    {
        if (owner == null || reservations == null || reservations.Count == 0)
        {
            return;
        }

        foreach (var reservation in reservations)
        {
            if (!owner.TryGiveItem(
                    reservation.ItemId,
                    reservation.Quantity,
                    reservation.Properties,
                    ItemHandling.Normal,
                    slot: reservation.SlotIndex
                ))
            {
                Log.Warning(
                    "Failed to restore item {ItemId} x{Quantity} to slot {Slot} for player {PlayerId}.",
                    reservation.ItemId,
                    reservation.Quantity,
                    reservation.SlotIndex,
                    owner.Id
                );
            }
        }

        try
        {
            owner.Save();
        }
        catch (Exception exception)
        {
            Log.Warning(
                exception,
                "Failed to save player {PlayerId} while restoring player shop inventory.",
                owner.Id
            );
        }
    }

    private static Dictionary<int, List<Guid>> BuildEquipmentSnapshot(Player? owner)
    {
        var slotCount = Options.Instance.Equipment.Slots.Count;
        var snapshot = new Dictionary<int, List<Guid>>(slotCount);

        for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            var equippedItems = new List<Guid>();

            if (owner?.Equipment != null && owner.Equipment.TryGetValue(slotIndex, out var inventorySlots))
            {
                foreach (var inventoryIndex in inventorySlots ?? Enumerable.Empty<int>())
                {
                    if (inventoryIndex < 0 || inventoryIndex >= owner.Items.Count)
                    {
                        continue;
                    }

                    var slot = owner.Items[inventoryIndex];
                    if (slot?.ItemId != Guid.Empty)
                    {
                        equippedItems.Add(slot.ItemId);
                    }
                }
            }

            snapshot[slotIndex] = equippedItems;
        }

        return snapshot;
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
            .FirstOrDefault(i => i.ShopId == shopId && i.Id == shopItemId && i.Shop.Status == PlayerShopStatus.Active);

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

    private static void RestorePendingGold(Guid shopId, long amount)
    {
        if (amount <= 0)
        {
            return;
        }

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var rows = context.Database.ExecuteSqlInterpolated(
            $"UPDATE Player_Shops SET PendingGold = PendingGold + {amount} WHERE Id = {shopId}"
        );

        if (rows <= 0)
        {
            return;
        }

        if (ActiveShops.TryGetValue(shopId, out var runtime))
        {
            runtime.UpdatePendingGold(runtime.PendingGold + amount);
        }
    }

    public static bool CloseShop(Guid shopId, PlayerShopStatus status = PlayerShopStatus.Closed)
    {
        if (ActiveShops.TryGetValue(shopId, out var activeRuntime))
        {
            lock (activeRuntime.SyncRoot)
            {
                return CloseShopCore(shopId, status);
            }
        }

        return CloseShopCore(shopId, status);
    }

    private static bool CloseShopCore(Guid shopId, PlayerShopStatus status)
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

        DespawnShopEntity(shopId);

        UpdatePlayerActiveShop(runtime?.OwnerId ?? shop.OwnerId, null, status);

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
            if (ActiveShops.TryGetValue(shop.Id, out var activeRuntime))
            {
                lock (activeRuntime.SyncRoot)
                {
                    ExpireShop(context, now, owners, shop);
                }
            }
            else
            {
                ExpireShop(context, now, owners, shop);
            }
        }

        context.SaveChanges();

        foreach (var ownerId in owners)
        {
            UpdatePlayerActiveShop(ownerId, null, PlayerShopStatus.Expired);
        }

        return expired.Count;
    }

    private static void ExpireShop(PlayerContext context, DateTime now, List<Guid> owners, PlayerShop shop)
    {
        var totalSalesGold = context.Player_ShopTransactions
            .Where(transaction => transaction.ShopId == shop.Id)
            .Sum(transaction => (long?)transaction.TotalPrice) ?? 0;

        shop.PendingGold = Math.Max(shop.PendingGold, totalSalesGold);
        shop.Status = PlayerShopStatus.Expired;
        shop.ClosedAt = now;
        owners.Add(shop.OwnerId);
        if (ActiveShops.TryRemove(shop.Id, out var runtime))
        {
            runtime.UpdateStatus(PlayerShopStatus.Expired);
            runtime.UpdatePendingGold(shop.PendingGold);
        }

        DespawnShopEntity(shop.Id);
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

    private sealed class SlotReservation
    {
        public SlotReservation(int slotIndex, InventorySlot slot, Guid itemId, ItemProperties properties)
        {
            SlotIndex = slotIndex;
            Slot = slot;
            ItemId = itemId;
            Properties = properties;
        }

        public int SlotIndex { get; }

        public InventorySlot Slot { get; }

        public Guid ItemId { get; }

        public ItemProperties Properties { get; }

        public int QuantityToTake { get; set; }
    }

    private sealed record ReservedInventoryItem(int SlotIndex, Guid ItemId, int Quantity, ItemProperties Properties);

    public readonly record struct PlayerShopStock(
        Guid ItemId,
        int Quantity,
        int PricePerUnit,
        ItemProperties? Properties,
        int? InventorySlot = null
    )
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

    public sealed record PlayerShopLiquidationSummary(
        Guid ShopId,
        string ShopName,
        long GoldPaid,
        int ReturnedStackCount,
        int ReturnedItemQuantity
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

            runtime = new PlayerShopRuntime(entity, entity.Owner?.Name, entity.Owner);
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
                if (!DeliverCurrency(player, runtime.ShopId, currencyDescriptor.Id, goldPaid))
                {
                    RestorePendingGold(runtime.ShopId, goldPaid);
                    return false;
                }
            }

            if (unsoldItems.Count > 0 && !DeliverItems(player, runtime.ShopId, unsoldItems))
            {
                return false;
            }

            CloseShop(runtime.ShopId, PlayerShopStatus.Closed);
            DespawnShopEntity(runtime.ShopId);
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

    public static IReadOnlyList<PlayerShopLiquidationSummary> ApplyPendingLiquidations(Player player)
    {
        if (player == null)
        {
            return Array.Empty<PlayerShopLiquidationSummary>();
        }

        var summaries = new List<PlayerShopLiquidationSummary>();
        using var context = DbInterface.CreatePlayerContext(readOnly: false, explicitLoad: true);
        var shops = context.Player_Shops
            .Include(shop => shop.Items)
            .Where(
                shop =>
                    shop.OwnerId == player.Id
                    && shop.Status == PlayerShopStatus.Expired
                    && shop.LiquidatedAt == null
            )
            .ToList();

        if (shops.Count == 0)
        {
            return summaries;
        }

        var currencyDescriptor = ResolveGlobalCurrencyDescriptor();
        foreach (var shop in shops)
        {
            var unsoldItems = shop.Items
                .Where(item => !item.IsSold && item.Quantity > 0)
                .Select(item => new Item(item.ItemId, item.Quantity) { Properties = new ItemProperties(item.Properties) })
                .ToList();

            var returnedStackCount = unsoldItems.Count;
            var returnedItemQuantity = unsoldItems.Sum(item => item.Quantity);
            var goldPaid = 0L;
            var pendingGold = Math.Max(0, shop.PendingGold);

            if (pendingGold > 0)
            {
                if (currencyDescriptor == null)
                {
                    Log.Warning(
                        "Skipping expired shop liquidation for {ShopId}: no global currency configured.",
                        shop.Id
                    );

                    continue;
                }

                if (!DeliverCurrency(player, shop.Id, currencyDescriptor.Id, pendingGold))
                {
                    Log.Warning(
                        "Skipping expired shop liquidation for {ShopId}: failed to deliver currency to owner {OwnerId}.",
                        shop.Id,
                        player.Id
                    );

                    continue;
                }

                goldPaid = pendingGold;
                shop.PendingGold = 0;
            }

            if (unsoldItems.Count > 0 && !DeliverItems(player, shop.Id, unsoldItems))
            {
                if (goldPaid > 0)
                {
                    shop.PendingGold = goldPaid;
                }

                Log.Warning(
                    "Skipping expired shop liquidation for {ShopId}: failed to deliver unsold items to owner {OwnerId}.",
                    shop.Id,
                    player.Id
                );

                continue;
            }

            var now = DateTime.UtcNow;
            shop.LiquidatedAt = now;
            shop.LiquidatedGold = goldPaid;
            shop.Status = PlayerShopStatus.Closed;
            shop.ClosedAt ??= now;

            UpdatePlayerActiveShop(
                shop.OwnerId,
                null,
                PlayerShopStatus.Closed,
                owner: player,
                context,
                saveChanges: false
            );

            ActiveShops.TryRemove(shop.Id, out _);
            DespawnShopEntity(shop.Id);

            summaries.Add(
                new PlayerShopLiquidationSummary(
                    shop.Id,
                    shop.Title,
                    goldPaid,
                    returnedStackCount,
                    returnedItemQuantity
                )
            );
        }

        if (summaries.Count > 0)
        {
            context.SaveChanges();
        }

        return summaries;
    }

    public sealed class PlayerShopRuntime
    {
        private readonly Dictionary<Guid, PlayerShopItemRuntime> _items;
        private readonly Dictionary<int, List<Guid>> _equipment;

        internal PlayerShopRuntime(PlayerShop shop, string? ownerName = null, Player? owner = null)
        {
            ShopId = shop.Id;
            OwnerId = shop.OwnerId;
            MapId = shop.MapId;
            MapInstanceId = shop.MapInstanceId;
            X = shop.X;
            Y = shop.Y;
            Z = shop.Z;
            Title = shop.Title;
            OwnerName = ownerName ?? shop.Owner?.Name ?? string.Empty;
            Status = shop.Status;
            PendingGold = shop.PendingGold;
            CreatedAt = shop.CreatedAt;
            ExpiresAt = shop.ExpiresAt;
            Decoration = string.IsNullOrWhiteSpace(shop.Decoration)
                ? PlayerShopEntityConstants.DefaultDecoration
                : shop.Decoration;

            var appearanceSource = owner ?? shop.Owner;
            Sprite = !string.IsNullOrWhiteSpace(appearanceSource?.Sprite)
                ? appearanceSource.Sprite
                : PlayerShopEntityConstants.DefaultSprite;
            Face = appearanceSource?.Face ?? string.Empty;
            Color = appearanceSource?.Color ?? Color.White;
            Gender = appearanceSource?.Gender ?? Gender.Male;
            _equipment = BuildEquipmentSnapshot(appearanceSource);

            _items = shop.Items?.ToDictionary(item => item.Id, item => new PlayerShopItemRuntime(item))
                ?? new Dictionary<Guid, PlayerShopItemRuntime>();
        }

        internal object SyncRoot { get; } = new();

        public Guid ShopId { get; }

        public Guid OwnerId { get; }

        public string OwnerName { get; }

        public Guid MapId { get; }

        public Guid MapInstanceId { get; }

        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        public string Title { get; }

        public string Decoration { get; }

        public string Sprite { get; }

        public string Face { get; }

        public Color Color { get; }

        public Gender Gender { get; }

        public IReadOnlyDictionary<int, List<Guid>> Equipment => _equipment;

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

    private static bool DeliverCurrency(Player player, Guid shopId, Guid currencyItemId, long amount)
    {
        if (player == null || amount <= 0)
        {
            return true;
        }

        var remaining = amount;
        var stacks = new List<Item>();

        while (remaining > 0)
        {
            var chunk = (int)Math.Min(remaining, int.MaxValue);
            stacks.Add(new Item(currencyItemId, chunk));
            remaining -= chunk;
        }

        return DeliverItems(player, shopId, stacks);
    }

    private static bool DeliverItems(Player player, Guid shopId, IEnumerable<Item> items)
    {
        if (player == null)
        {
            return false;
        }

        var success = true;
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

            success = false;
            Log.Warning(
                "Failed to deliver item {ItemId} x{Quantity} to player {PlayerId} while closing shop {ShopId}",
                item.ItemId,
                item.Quantity,
                player.Id,
                shopId
            );
        }

        return success;
    }
}
