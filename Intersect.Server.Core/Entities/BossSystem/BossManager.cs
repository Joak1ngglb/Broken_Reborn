using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Intersect.Utilities;
using Newtonsoft.Json;

namespace Intersect.Server.Entities.BossSystem;

public static class BossManager
{
    public static BossManagerService Instance { get; } = new();

    public static void InitializeCatalog() => Instance.Initialize();

    public static void RegisterSpawn(Npc npc) => Instance.RegisterSpawn(npc);

    public static void HandleDeath(Npc npc) => Instance.HandleDeath(npc);

    public static void Update(long now) => Instance.CheckRespawns(now);

    public static string ExportDebugJson() => Instance.ExportDebugJson();
}

public sealed class BossManagerService
{
    private readonly ConcurrentDictionary<Guid, BossCatalogEntry> _catalog = new();
    private readonly ConcurrentDictionary<Guid, BossRuntimeState> _runtime = new();
    private readonly SemaphoreSlim _respawnSemaphore = new(1, 1);
    private readonly object _timerLock = new();

    private Timer? _respawnTimer;
    private int _initialized;

    public void Initialize()
    {
        _catalog.Clear();
        _runtime.Clear();

        var bosses = NPCDescriptor.Lookup.Values
            .OfType<NPCDescriptor>()
            .Where(npc => npc?.IsBoss == true)
            .ToArray();

        foreach (var boss in bosses)
        {
            if (!TryResolveSpawn(boss.Id, out var mapId, out var spawn))
            {
                ApplicationContext.Context.Value?.Logger.LogWarning(
                    "Boss config inválida: npc {BossId} ({BossName}) marcado como boss pero sin spawn válido en mapa.",
                    boss.Id,
                    boss.Name
                );

                continue;
            }

            _catalog[boss.Id] = new BossCatalogEntry(
                boss.Id,
                mapId,
                new NpcSpawn(spawn),
                boss.BossRespawnMinutes,
                boss.BossAnnounceOnKill,
                boss.BossAnnounceOnRespawn
            );

            _runtime[boss.Id] = new BossRuntimeState
            {
                NpcId = boss.Id,
                IsAlive = false,
                NextRespawnAt = null,
                AliveEntityId = null,
                MapId = mapId,
                MapInstanceId = Guid.Empty,
            };
        }

        StartRespawnTimer();
        Interlocked.Exchange(ref _initialized, 1);

        ApplicationContext.Context.Value?.Logger.LogInformation(
            "BossManager inicializado. Bosses en catálogo: {BossCount}",
            _catalog.Count
        );
    }

    public void Shutdown()
    {
        StopRespawnTimer();
        Interlocked.Exchange(ref _initialized, 0);

        ApplicationContext.Context.Value?.Logger.LogInformation("BossManager apagado correctamente.");
    }

    public bool IsBoss(Guid npcId)
    {
        EnsureInitialized();
        return _catalog.ContainsKey(npcId);
    }

    public async Task OnBossKilled(Guid npcId, Player player)
    {
        EnsureInitialized();

        if (!_catalog.TryGetValue(npcId, out var catalogEntry))
        {
            ApplicationContext.Context.Value?.Logger.LogWarning(
                "OnBossKilled ignorado: npc {BossId} no existe en catálogo.",
                npcId
            );

            return;
        }

        if (_runtime.TryGetValue(npcId, out var state))
        {
            ApplicationContext.Context.Value?.Logger.LogInformation(
                "Boss kill registrado: {BossId} por {PlayerName} ({PlayerId}). Próximo respawn: {NextRespawnAt}.",
                npcId,
                player?.Name,
                player?.Id,
                state.NextRespawnAt
            );
        }
        else
        {
            ApplicationContext.Context.Value?.Logger.LogWarning(
                "Boss kill registrado sin estado runtime: {BossId} por {PlayerName} ({PlayerId}).",
                npcId,
                player?.Name,
                player?.Id
            );
        }

        if (catalogEntry.AnnounceOnKill)
        {
            PacketSender.SendGlobalMsg($"Boss defeated: {NPCDescriptor.Get(npcId)?.Name ?? npcId.ToString()}");
        }

        await Task.CompletedTask;
    }

    public void RegisterSpawn(Npc npc)
    {
        EnsureInitialized();

        var descriptor = npc?.Descriptor;
        if (descriptor?.IsBoss != true || !_catalog.ContainsKey(descriptor.Id))
        {
            return;
        }

        _runtime.AddOrUpdate(
            descriptor.Id,
            _ => new BossRuntimeState
            {
                NpcId = descriptor.Id,
                IsAlive = true,
                NextRespawnAt = null,
                AliveEntityId = npc.Id,
                MapId = npc.MapId,
                MapInstanceId = npc.MapInstanceId,
            },
            (_, state) =>
            {
                state.IsAlive = true;
                state.NextRespawnAt = null;
                state.AliveEntityId = npc.Id;
                state.MapId = npc.MapId;
                state.MapInstanceId = npc.MapInstanceId;

                return state;
            }
        );

        ApplicationContext.Context.Value?.Logger.LogInformation(
            "Boss spawn detectado: {BossId} ({BossName}) en mapa {MapId} instancia {MapInstanceId} entidad {EntityId}.",
            descriptor.Id,
            descriptor.Name,
            npc.MapId,
            npc.MapInstanceId,
            npc.Id
        );
    }

    public void HandleDeath(Npc npc)
    {
        EnsureInitialized();

        var descriptor = npc?.Descriptor;
        if (descriptor?.IsBoss != true || !_catalog.TryGetValue(descriptor.Id, out var catalogEntry))
        {
            return;
        }

        var nextRespawnAt = Timing.Global.Milliseconds + GetRespawnDelayMs(descriptor, catalogEntry);

        _runtime.AddOrUpdate(
            descriptor.Id,
            _ => new BossRuntimeState
            {
                NpcId = descriptor.Id,
                IsAlive = false,
                NextRespawnAt = nextRespawnAt,
                AliveEntityId = null,
                MapId = npc.MapId,
                MapInstanceId = npc.MapInstanceId,
            },
            (_, state) =>
            {
                state.IsAlive = false;
                state.NextRespawnAt = nextRespawnAt;
                state.AliveEntityId = null;
                state.MapId = npc.MapId;
                state.MapInstanceId = npc.MapInstanceId;

                return state;
            }
        );

        ApplicationContext.Context.Value?.Logger.LogInformation(
            "Boss death registrado: {BossId} ({BossName}) en mapa {MapId}/{MapInstanceId}. Respawn en {NextRespawnAt}.",
            descriptor.Id,
            descriptor.Name,
            npc.MapId,
            npc.MapInstanceId,
            nextRespawnAt
        );
    }

    public void CheckRespawns(long now)
    {
        EnsureInitialized();

        if (!_respawnSemaphore.Wait(0))
        {
            return;
        }

        try
        {
            foreach (var (bossId, state) in _runtime)
            {
                if (state.IsAlive || !state.NextRespawnAt.HasValue || state.NextRespawnAt.Value > now)
                {
                    continue;
                }

                if (!_catalog.TryGetValue(bossId, out var catalogEntry))
                {
                    ApplicationContext.Context.Value?.Logger.LogWarning(
                        "Respawn omitido: {BossId} no está en catálogo.",
                        bossId
                    );

                    continue;
                }

                if (!MapController.TryGetInstanceFromMap(state.MapId, state.MapInstanceId, out var mapInstance))
                {
                    ApplicationContext.Context.Value?.Logger.LogWarning(
                        "Respawn fallido: boss {BossId} no encontró mapa/instancia {MapId}/{MapInstanceId}.",
                        bossId,
                        state.MapId,
                        state.MapInstanceId
                    );

                    continue;
                }

                ResolveSpawnPosition(mapInstance, catalogEntry.Spawn, out var x, out var y, out var direction);
                var spawnedNpc = mapInstance.SpawnNpc((byte)x, (byte)y, direction, bossId);
                if (spawnedNpc == null)
                {
                    ApplicationContext.Context.Value?.Logger.LogWarning(
                        "Respawn fallido: SpawnNpc devolvió null para boss {BossId} en mapa {MapId}/{MapInstanceId}.",
                        bossId,
                        state.MapId,
                        state.MapInstanceId
                    );

                    continue;
                }

                RegisterSpawn(spawnedNpc);
                ApplicationContext.Context.Value?.Logger.LogInformation(
                    "Boss respawneado: {BossId} ({BossName}) en {MapId}/{MapInstanceId} entidad {EntityId}.",
                    bossId,
                    spawnedNpc.Name,
                    state.MapId,
                    state.MapInstanceId,
                    spawnedNpc.Id
                );

                if (catalogEntry.AnnounceOnRespawn)
                {
                    PacketSender.SendGlobalMsg($"Boss respawned: {spawnedNpc.Name}");
                }
            }
        }
        catch (Exception exception)
        {
            ApplicationContext.Context.Value?.Logger.LogError(exception, "Error al ejecutar ciclo de respawn de bosses.");
        }
        finally
        {
            _respawnSemaphore.Release();
        }
    }

    public string ExportDebugJson()
    {
        EnsureInitialized();
        return JsonConvert.SerializeObject(_runtime.Values.OrderBy(state => state.NpcId).ToArray(), Formatting.Indented);
    }

    private void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized) != 1)
        {
            Initialize();
        }
    }

    private void StartRespawnTimer()
    {
        lock (_timerLock)
        {
            _respawnTimer?.Dispose();
            _respawnTimer = new Timer(_ => CheckRespawns(Timing.Global.Milliseconds), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    private void StopRespawnTimer()
    {
        lock (_timerLock)
        {
            _respawnTimer?.Dispose();
            _respawnTimer = null;
        }
    }

    private static bool TryResolveSpawn(Guid npcId, out Guid mapId, out NpcSpawn spawn)
    {
        foreach (var map in MapController.Lookup.Values.OfType<MapController>())
        {
            var resolvedSpawn = map.Spawns.FirstOrDefault(existingSpawn => existingSpawn.NpcId == npcId);
            if (resolvedSpawn == null)
            {
                continue;
            }

            mapId = map.Id;
            spawn = resolvedSpawn;

            return true;
        }

        mapId = Guid.Empty;
        spawn = new NpcSpawn();

        return false;
    }

    private static long GetRespawnDelayMs(NPCDescriptor descriptor, BossCatalogEntry catalogEntry) =>
        catalogEntry.RespawnMinutes > 0
            ? catalogEntry.RespawnMinutes * 60000L
            : Math.Max(descriptor.SpawnDuration, 0);

    private static void ResolveSpawnPosition(MapInstance mapInstance, NpcSpawn spawn, out int x, out int y, out Direction direction)
    {
        direction = spawn.Direction != NpcSpawnDirection.Random
            ? (Direction)(spawn.Direction - 1)
            : Randomization.NextDirection();

        if (spawn.X >= 0 && spawn.Y >= 0)
        {
            x = spawn.X;
            y = spawn.Y;
            return;
        }

        var mapController = MapController.Get(mapInstance.MapId);
        x = 0;
        y = 0;

        for (var i = 0; i < 100; i++)
        {
            x = Randomization.Next(0, Options.Instance.Map.MapWidth);
            y = Randomization.Next(0, Options.Instance.Map.MapHeight);

            if (mapController?.Attributes[x, y] == null ||
                mapController.Attributes[x, y].Type == (int)MapAttributeType.Walkable)
            {
                return;
            }
        }

        x = 0;
        y = 0;
    }
}
