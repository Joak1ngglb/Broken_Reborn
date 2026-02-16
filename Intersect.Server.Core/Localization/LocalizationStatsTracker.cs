using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Server.Localization;

public static class LocalizationStatsTracker
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    private static readonly ConcurrentQueue<LocalizationStatsEvent> Events = new();

    public static void RecordPacketBatch(
        int totalRequests,
        int validRequests,
        int emptyRequests,
        int unknownEntityTypeRequests,
        IReadOnlyDictionary<string, int>? missesByEntityTypeAndField
    )
    {
        Enqueue(
            new LocalizationStatsEvent(
                DateTime.UtcNow,
                totalRequests,
                validRequests,
                emptyRequests,
                unknownEntityTypeRequests,
                0,
                0,
                missesByEntityTypeAndField?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, int>()
            )
        );
    }

    public static void RecordRepositoryLookup(bool missingSource, bool missingCurrentHashTranslation)
    {
        if (!missingSource && !missingCurrentHashTranslation)
        {
            return;
        }

        Enqueue(
            new LocalizationStatsEvent(
                DateTime.UtcNow,
                0,
                0,
                0,
                0,
                missingSource ? 1 : 0,
                missingCurrentHashTranslation ? 1 : 0,
                new Dictionary<string, int>()
            )
        );
    }

    public static LocalizationStatsSnapshot GetSnapshot(TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - (window <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : window);
        Prune(now);

        var relevantEvents = Events.Where(@event => @event.TimestampUtc >= cutoff).ToArray();
        var missesByEntityTypeAndField = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var @event in relevantEvents)
        {
            foreach (var pair in @event.MissesByEntityTypeAndField)
            {
                missesByEntityTypeAndField[pair.Key] =
                    missesByEntityTypeAndField.TryGetValue(pair.Key, out var count)
                        ? count + pair.Value
                        : pair.Value;
            }
        }

        return new LocalizationStatsSnapshot(
            (int)Math.Round((now - cutoff).TotalMinutes),
            relevantEvents.Sum(@event => (long)@event.TotalRequests),
            relevantEvents.Sum(@event => (long)@event.ValidRequests),
            relevantEvents.Sum(@event => (long)@event.EmptyRequests),
            relevantEvents.Sum(@event => (long)@event.UnknownEntityTypeRequests),
            relevantEvents.Sum(@event => (long)@event.RepositoryMissingSourceCount),
            relevantEvents.Sum(@event => (long)@event.RepositoryMissingCurrentHashTranslationCount),
            missesByEntityTypeAndField
        );
    }

    private static void Enqueue(LocalizationStatsEvent @event)
    {
        Events.Enqueue(@event);
        Prune(@event.TimestampUtc);
    }

    private static void Prune(DateTime now)
    {
        var cutoff = now - Retention;
        while (Events.TryPeek(out var @event) && @event.TimestampUtc < cutoff)
        {
            Events.TryDequeue(out _);
        }
    }

    private sealed record LocalizationStatsEvent(
        DateTime TimestampUtc,
        int TotalRequests,
        int ValidRequests,
        int EmptyRequests,
        int UnknownEntityTypeRequests,
        int RepositoryMissingSourceCount,
        int RepositoryMissingCurrentHashTranslationCount,
        IReadOnlyDictionary<string, int> MissesByEntityTypeAndField
    );
}

public sealed record LocalizationStatsSnapshot(
    int WindowMinutes,
    long TotalRequests,
    long ValidRequests,
    long EmptyRequests,
    long UnknownEntityTypeRequests,
    long RepositoryMissingSourceCount,
    long RepositoryMissingCurrentHashTranslationCount,
    IReadOnlyDictionary<string, long> MissesByEntityTypeAndField
);
