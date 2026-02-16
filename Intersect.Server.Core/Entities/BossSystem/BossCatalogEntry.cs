using Intersect.Framework.Core.GameObjects.Maps;

namespace Intersect.Server.Entities.BossSystem;

public sealed record BossCatalogEntry(
    Guid NpcId,
    Guid MapId,
    NpcSpawn Spawn,
    int RespawnMinutes,
    bool AnnounceOnKill,
    bool AnnounceOnRespawn
);
