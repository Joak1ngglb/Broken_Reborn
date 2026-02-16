using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Intersect.Utilities;
using Newtonsoft.Json;

namespace Intersect.Server.Entities.BossSystem;

public static class BossManager
{
    private static readonly ConcurrentDictionary<Guid, BossCatalogEntry> Catalog = new();
    private static readonly ConcurrentDictionary<Guid, BossRuntimeState> Runtime = new();
    private static int _initialized;

    public static void InitializeCatalog()
    {
        Catalog.Clear();
        Runtime.Clear();

        var bosses = NPCDescriptor.Lookup.Values
            .OfType<NPCDescriptor>()
            .Where(npc => npc?.IsBoss == true)
            .ToArray();

        foreach (var boss in bosses)
        {
            if (!TryResolveSpawn(boss.Id, out var mapId, out var spawn))
            {
                ApplicationContext.Context.Value?.Logger.LogWarning(
                    "Boss {BossId} ({BossName}) has IsBoss=true but is not assigned to any map spawn.",
                    boss.Id,
                    boss.Name
                );

                continue;
            }

            Catalog[boss.Id] = new BossCatalogEntry(
                boss.Id,
                mapId,
                new NpcSpawn(spawn),
                boss.BossRespawnMinutes,
                boss.BossAnnounceOnKill,
                boss.BossAnnounceOnRespawn
            );

            Runtime[boss.Id] = new BossRuntimeState
            {
                NpcId = boss.Id,
                IsAlive = false,
                NextRespawnAt = null,
                AliveEntityId = null,
                MapId = mapId,
                MapInstanceId = Guid.Empty,
            };
        }

        Interlocked.Exchange(ref _initialized, 1);

        ApplicationContext.Context.Value?.Logger.LogInformation(
            "BossManager initialized with {Count} bosses from NPCDescriptor.Lookup.",
            Catalog.Count
        );
    }

    public static void RegisterSpawn(Npc npc)
    {
        EnsureInitialized();

        var descriptor = npc?.Descriptor;
        if (descriptor?.IsBoss != true || !Catalog.ContainsKey(descriptor.Id))
        {
            return;
        }

        Runtime.AddOrUpdate(
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
    }

    public static void HandleDeath(Npc npc)
    {
        EnsureInitialized();

        var descriptor = npc?.Descriptor;
        if (descriptor?.IsBoss != true || !Catalog.TryGetValue(descriptor.Id, out var catalogEntry))
        {
            return;
        }

        var nextRespawnAt = Timing.Global.Milliseconds + GetRespawnDelayMs(descriptor, catalogEntry);

        Runtime.AddOrUpdate(
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

        if (catalogEntry.AnnounceOnKill)
        {
            PacketSender.SendGlobalMsg($"Boss defeated: {descriptor.Name}");
        }
    }

    public static void Update(long now)
    {
        EnsureInitialized();

        foreach (var (bossId, state) in Runtime)
        {
            if (state.IsAlive || !state.NextRespawnAt.HasValue || state.NextRespawnAt.Value > now)
            {
                continue;
            }

            if (!Catalog.TryGetValue(bossId, out var catalogEntry))
            {
                continue;
            }

            if (!MapController.TryGetInstanceFromMap(state.MapId, state.MapInstanceId, out var mapInstance))
            {
                continue;
            }

            ResolveSpawnPosition(mapInstance, catalogEntry.Spawn, out var x, out var y, out var direction);
            var spawnedNpc = mapInstance.SpawnNpc((byte)x, (byte)y, direction, bossId);
            if (spawnedNpc == null)
            {
                continue;
            }

            RegisterSpawn(spawnedNpc);
            if (catalogEntry.AnnounceOnRespawn)
            {
                PacketSender.SendGlobalMsg($"Boss respawned: {spawnedNpc.Name}");
            }
        }
    }

    public static string ExportDebugJson()
    {
        EnsureInitialized();
        return JsonConvert.SerializeObject(Runtime.Values.OrderBy(state => state.NpcId).ToArray(), Formatting.Indented);
    }

    private static void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized) != 1)
        {
            InitializeCatalog();
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
