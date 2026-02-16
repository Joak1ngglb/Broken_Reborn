using System;
using System.Collections.Concurrent;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Server.General;

public static class NpcBossCompatibility
{
    private const string LegacyBossPrefix = "[BOSS]";
    private static readonly ConcurrentDictionary<Guid, byte> WarnedNpcs = new();

    public static bool IsBoss(NPCDescriptor npcDescriptor)
    {
        if (npcDescriptor == null)
        {
            return false;
        }

        if (npcDescriptor.IsBoss)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(npcDescriptor.Name) &&
            npcDescriptor.Name.StartsWith(LegacyBossPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (WarnedNpcs.TryAdd(npcDescriptor.Id, 0))
            {
                ApplicationContext.Context.Value?.Logger.LogWarning(
                    "NPC {NpcId} ({NpcName}) still uses legacy boss name tag. Set IsBoss=true and remove the [BOSS] prefix.",
                    npcDescriptor.Id,
                    npcDescriptor.Name
                );
            }

            return true;
        }

        return false;
    }
}
