using System.Collections.Concurrent;

namespace Intersect.Server.Entities.BossSystem;

internal static class BossAiDebugSettings
{
    private static readonly ConcurrentDictionary<Guid, bool> BossDebugFlags = new();

    public static bool IsEnabled(Guid bossId)
    {
        return BossDebugFlags.TryGetValue(bossId, out var isEnabled) && isEnabled;
    }

    public static bool SetEnabled(Guid bossId, bool enabled)
    {
        if (enabled)
        {
            BossDebugFlags[bossId] = true;
            return true;
        }

        BossDebugFlags.TryRemove(bossId, out _);
        return false;
    }

    public static bool Toggle(Guid bossId)
    {
        var next = !IsEnabled(bossId);
        SetEnabled(bossId, next);
        return next;
    }
}
