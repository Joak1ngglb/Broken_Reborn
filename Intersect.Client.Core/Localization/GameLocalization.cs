using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Intersect.Client.General;
using Intersect.Network.Packets.Localization;
using Intersect.Client.Networking;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Localization;

public static class GameLocalization
{
    private static readonly TimeSpan MissingEntryRetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PendingRequestRetryDelay = TimeSpan.FromSeconds(5);

    private static readonly ConcurrentDictionary<LocalizationCacheKey, LocalizationCacheEntry> Cache = new();
    private static readonly ConcurrentDictionary<LocalizationCacheKey, DateTime> PendingRequests = new();

    private static readonly object StatsSync = new();
    private static readonly TimeSpan StatsLogThrottle = TimeSpan.FromSeconds(30);
    private static DateTime NextStatsLogUtc = DateTime.UtcNow + StatsLogThrottle;

    private static long CacheHits;
    private static long CacheMisses;
    private static long RequestsDispatched;

    public static event Action<string, IReadOnlyCollection<LocalizationRequestEntry>>? LocalizedTextsUpdated;

    public static string GetTextOrDefault(string entityType, Guid entityId, string field, string fallback)
    {
        var language = NormalizeLanguage(Globals.Database.Language);
        var key = new LocalizationCacheKey(entityType, entityId.ToString(), field, language);
        if (Cache.TryGetValue(key, out var cacheEntry))
        {
            if (cacheEntry.State == LocalizationCacheState.Resolved)
            {
                Interlocked.Increment(ref CacheHits);
                LogStatsIfNeeded();
                return string.IsNullOrWhiteSpace(cacheEntry.Text) ? fallback : cacheEntry.Text;
            }

            Interlocked.Increment(ref CacheMisses);
            LogStatsIfNeeded();

            RequestMissingEntries(
                language,
                [
                    new LocalizationRequestEntry(entityType, entityId.ToString(), field)
                ]
            );

            return fallback;
        }

        Interlocked.Increment(ref CacheMisses);
        LogStatsIfNeeded();

        RequestMissingEntries(
            language,
            [
                new LocalizationRequestEntry(entityType, entityId.ToString(), field)
            ]
        );

        return fallback;
    }

    public static void RequestEntries(IEnumerable<LocalizationRequestEntry> requests)
    {
        var language = NormalizeLanguage(Globals.Database.Language);
        RequestMissingEntries(language, requests);
    }

    public static void ApplyLocalizedTexts(string language, IEnumerable<LocalizedTextEntry> entries)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var updatedRequests = new List<LocalizationRequestEntry>();

        foreach (var entry in entries)
        {
            if (entry?.Request == null)
            {
                continue;
            }

            var request = entry.Request;
            var key = new LocalizationCacheKey(request.EntityType, request.EntityId, request.Field, normalizedLanguage);
            PendingRequests.TryRemove(key, out _);

            if (string.IsNullOrEmpty(entry.Text))
            {
                Cache[key] = new LocalizationCacheEntry(LocalizationCacheState.Missing, string.Empty, DateTime.UtcNow);
                continue;
            }

            Cache[key] = new LocalizationCacheEntry(LocalizationCacheState.Resolved, entry.Text, DateTime.MinValue);
            updatedRequests.Add(request);
        }

        if (updatedRequests.Count > 0)
        {
            LocalizedTextsUpdated?.Invoke(normalizedLanguage, updatedRequests);
        }
    }

    public static void Clear()
    {
        Cache.Clear();
        PendingRequests.Clear();
        Interlocked.Exchange(ref CacheHits, 0);
        Interlocked.Exchange(ref CacheMisses, 0);
        Interlocked.Exchange(ref RequestsDispatched, 0);
        lock (StatsSync)
        {
            NextStatsLogUtc = DateTime.UtcNow + StatsLogThrottle;
        }
    }

    private static void RequestMissingEntries(string language, IEnumerable<LocalizationRequestEntry> requests)
    {
        var now = DateTime.UtcNow;
        var pendingRequests = new List<LocalizationRequestEntry>();
        foreach (var request in requests)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.EntityType) ||
                string.IsNullOrWhiteSpace(request.EntityId) ||
                string.IsNullOrWhiteSpace(request.Field))
            {
                continue;
            }

            var key = new LocalizationCacheKey(request.EntityType, request.EntityId, request.Field, language);
            if (Cache.TryGetValue(key, out var cachedEntry))
            {
                if (cachedEntry.State == LocalizationCacheState.Resolved)
                {
                    continue;
                }

                if (now - cachedEntry.LastRequestUtc < MissingEntryRetryDelay)
                {
                    continue;
                }
            }

            if (PendingRequests.TryGetValue(key, out var lastPendingRequestAt) &&
                now - lastPendingRequestAt < PendingRequestRetryDelay)
            {
                continue;
            }

            PendingRequests[key] = now;
            Cache.AddOrUpdate(
                key,
                _ => new LocalizationCacheEntry(LocalizationCacheState.Missing, string.Empty, now),
                (_, existing) => new LocalizationCacheEntry(
                    existing.State == LocalizationCacheState.Resolved ? LocalizationCacheState.Resolved : LocalizationCacheState.Missing,
                    existing.Text,
                    existing.State == LocalizationCacheState.Resolved ? existing.LastRequestUtc : now
                )
            );
            pendingRequests.Add(request);
        }

        if (pendingRequests.Count > 0)
        {
            Interlocked.Add(ref RequestsDispatched, pendingRequests.Count);
            PacketSender.SendLocalizedTextRequest(language, pendingRequests);
            LogStatsIfNeeded();
        }
    }


    private static void LogStatsIfNeeded()
    {
        var now = DateTime.UtcNow;
        lock (StatsSync)
        {
            if (now < NextStatsLogUtc)
            {
                return;
            }

            var hits = Interlocked.Exchange(ref CacheHits, 0);
            var misses = Interlocked.Exchange(ref CacheMisses, 0);
            var dispatched = Interlocked.Exchange(ref RequestsDispatched, 0);
            NextStatsLogUtc = now + StatsLogThrottle;

            if (hits + misses + dispatched <= 0)
            {
                return;
            }

            ApplicationContext.Context.Value?.Logger.LogDebug(
                "Localization cache stats (last {WindowSeconds}s): hits={Hits}, misses={Misses}, requestsDispatched={RequestsDispatched}.",
                (int)StatsLogThrottle.TotalSeconds,
                hits,
                misses,
                dispatched
            );
        }
    }

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language)
            ? "en"
            : language.Trim().ToLowerInvariant();

    private readonly record struct LocalizationCacheKey(
        string EntityType,
        string EntityId,
        string Field,
        string Language
    );

    private readonly record struct LocalizationCacheEntry(
        LocalizationCacheState State,
        string Text,
        DateTime LastRequestUtc
    );

    private enum LocalizationCacheState
    {
        Missing,
        Resolved
    }
}
