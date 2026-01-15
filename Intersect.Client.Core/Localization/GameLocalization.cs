using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Intersect.Client.General;
using Intersect.Network.Packets.Localization;
using Intersect.Client.Networking;

namespace Intersect.Client.Localization;

public static class GameLocalization
{
    private static readonly ConcurrentDictionary<LocalizationCacheKey, string> Cache = new();
    private static readonly ConcurrentDictionary<LocalizationCacheKey, byte> PendingRequests = new();

    public static event Action<string, IReadOnlyCollection<LocalizationRequestEntry>>? LocalizedTextsUpdated;

    public static string GetTextOrDefault(string entityType, Guid entityId, string field, string fallback)
    {
        var language = NormalizeLanguage(Globals.Database.Language);
        var key = new LocalizationCacheKey(entityType, entityId.ToString(), field, language);
        if (Cache.TryGetValue(key, out var text))
        {
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

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
            Cache[key] = entry.Text ?? string.Empty;
            PendingRequests.TryRemove(key, out _);
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
    }

    private static void RequestMissingEntries(string language, IEnumerable<LocalizationRequestEntry> requests)
    {
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
            if (Cache.ContainsKey(key) || PendingRequests.ContainsKey(key))
            {
                continue;
            }

            PendingRequests[key] = 0;
            pendingRequests.Add(request);
        }

        if (pendingRequests.Count > 0)
        {
            PacketSender.SendLocalizedTextRequest(language, pendingRequests);
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
}
