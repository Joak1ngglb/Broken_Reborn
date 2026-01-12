using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Intersect.Core;
using Intersect.Server.Core;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Intersect.Server.Services;

public static class ServerTranslationService
{
    private const string CacheVersion = "v1";
    private const int MaxBatchSize = 20;
    private const int MaxBatchCharacters = 2000;
    private const int MaxCharsPerMinutePerAccount = 20000;
    private const int MaxRequestsPerMinutePerIp = 60;

    private static readonly TranslationCache Cache = new();
    private static readonly ConcurrentDictionary<string, FixedWindowRateLimiter> AccountCharLimiters = new();
    private static readonly ConcurrentDictionary<string, FixedWindowRateLimiter> IpRequestLimiters = new();

    public static async Task<IReadOnlyList<string>> TranslateBatch(
        string targetLang,
        string? sourceLang,
        IReadOnlyList<string> texts,
        string scope,
        string? accountId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (texts.Count == 0)
        {
            return Array.Empty<string>();
        }

        var requests = new List<TranslationRequest>(texts.Count);
        for (var i = 0; i < texts.Count; i++)
        {
            var text = texts[i] ?? string.Empty;
            var detectedSource = sourceLang ?? DetectLanguage(text);
            requests.Add(new TranslationRequest(i, text, detectedSource));
        }

        await Cache.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var cacheKeys = requests
            .Select(request => BuildCacheKey(scope, request.SourceLang, targetLang, request.Text))
            .ToArray();
        var cachedTranslations = await Cache.FetchAsync(cacheKeys, cancellationToken).ConfigureAwait(false);

        var hits = 0;
        var misses = 0;
        var output = new string[texts.Count];
        var pending = new List<TranslationWorkItem>();

        for (var i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            var cacheKey = cacheKeys[i];
            if (cachedTranslations.TryGetValue(cacheKey, out var cachedText))
            {
                output[request.Index] = cachedText;
                hits++;
            }
            else
            {
                pending.Add(new TranslationWorkItem(request, cacheKey));
                misses++;
            }
        }

        LogCacheStats(scope, hits, misses);

        if (pending.Count == 0)
        {
            return output;
        }

        var resolvedAccount = string.IsNullOrWhiteSpace(accountId) ? scope : accountId;
        var resolvedIp = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress;
        var charBudget = AccountCharLimiters.GetOrAdd(
            resolvedAccount,
            _ => new FixedWindowRateLimiter(TimeSpan.FromMinutes(1), MaxCharsPerMinutePerAccount)
        );
        var requestBudget = IpRequestLimiters.GetOrAdd(
            resolvedIp,
            _ => new FixedWindowRateLimiter(TimeSpan.FromMinutes(1), MaxRequestsPerMinutePerIp)
        );

        var pendingCharCount = pending.Sum(item => item.Request.Text.Length);
        if (!charBudget.TryConsume(pendingCharCount) || !requestBudget.TryConsume(1))
        {
            Logger?.LogWarning(
                "Translation request for scope {Scope} was rate limited (chars={Chars}, ip={Ip}).",
                scope,
                pendingCharCount,
                resolvedIp
            );

            foreach (var item in pending)
            {
                output[item.Request.Index] = item.Request.Text;
            }

            return output;
        }

        var workerCount = Math.Clamp(Environment.ProcessorCount, 2, 5);
        var batches = BuildBatches(pending);
        using var semaphore = new SemaphoreSlim(workerCount, workerCount);
        var tasks = batches
            .Select(batch => TranslateBatchWithLimitAsync(batch, targetLang, semaphore, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        var saveResults = new Dictionary<string, string>(pending.Count);
        foreach (var task in tasks)
        {
            foreach (var result in task.Result)
            {
                output[result.Index] = result.Translation;
                saveResults[result.CacheKey] = result.Translation;
            }
        }

        await Cache.StoreAsync(saveResults, cancellationToken).ConfigureAwait(false);

        return output;
    }

    private static string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "und";
        }

        foreach (var character in text)
        {
            if (character is >= '\u4e00' and <= '\u9fff')
            {
                return "zh";
            }

            if (character is >= '\u3040' and <= '\u30ff')
            {
                return "ja";
            }

            if (character is >= '\u0400' and <= '\u04ff')
            {
                return "ru";
            }
        }

        return "en";
    }

    private static string BuildCacheKey(string scope, string sourceLang, string targetLang, string text)
    {
        var hash = ComputeHash(text);
        return $"{scope}|{sourceLang}|{targetLang}|{hash}|{CacheVersion}";
    }

    private static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
        {
            builder.Append(value.ToString("x2"));
        }

        return builder.ToString();
    }

    private static IReadOnlyList<IReadOnlyList<TranslationWorkItem>> BuildBatches(
        IReadOnlyList<TranslationWorkItem> pending
    )
    {
        var batches = new List<IReadOnlyList<TranslationWorkItem>>();
        var current = new List<TranslationWorkItem>();
        var currentChars = 0;

        foreach (var item in pending)
        {
            var itemChars = item.Request.Text.Length;
            if (current.Count >= MaxBatchSize || (currentChars + itemChars) > MaxBatchCharacters)
            {
                if (current.Count > 0)
                {
                    batches.Add(current);
                    current = new List<TranslationWorkItem>();
                    currentChars = 0;
                }
            }

            current.Add(item);
            currentChars += itemChars;
        }

        if (current.Count > 0)
        {
            batches.Add(current);
        }

        return batches;
    }

    private static async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
        IReadOnlyList<TranslationWorkItem> batch,
        string targetLang,
        CancellationToken cancellationToken
    )
    {
        var translations = await TranslateWithProviderAsync(
            batch.Select(item => item.Request).ToArray(),
            targetLang,
            cancellationToken
        ).ConfigureAwait(false);

        var results = new List<TranslationResult>(batch.Count);
        for (var i = 0; i < batch.Count; i++)
        {
            results.Add(new TranslationResult(batch[i].Request.Index, translations[i], batch[i].CacheKey));
        }

        return results;
    }

    private static async Task<IReadOnlyList<TranslationResult>> TranslateBatchWithLimitAsync(
        IReadOnlyList<TranslationWorkItem> batch,
        string targetLang,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken
    )
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await TranslateBatchAsync(batch, targetLang, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static Task<IReadOnlyList<string>> TranslateWithProviderAsync(
        IReadOnlyList<TranslationRequest> requests,
        string targetLang,
        CancellationToken cancellationToken
    )
    {
        var translations = requests.Select(request => request.Text).ToArray();
        Logger?.LogDebug(
            "Translation provider not configured; returning source text for target {TargetLang}.",
            targetLang
        );
        return Task.FromResult<IReadOnlyList<string>>(translations);
    }

    private static void LogCacheStats(string scope, int hits, int misses)
    {
        Logger?.LogInformation(
            "Translation cache stats for scope {Scope}: hits={Hits}, misses={Misses}",
            scope,
            hits,
            misses
        );
    }

    private static ILogger? Logger => ApplicationContext.Context.Value?.Logger;

    private readonly record struct TranslationRequest(int Index, string Text, string SourceLang);

    private readonly record struct TranslationWorkItem(TranslationRequest Request, string CacheKey);

    private readonly record struct TranslationResult(int Index, string Translation, string CacheKey);

    private sealed class FixedWindowRateLimiter
    {
        private readonly TimeSpan _window;
        private readonly int _maxCount;
        private readonly object _lock = new();
        private DateTime _windowStart;
        private int _count;

        public FixedWindowRateLimiter(TimeSpan window, int maxCount)
        {
            _window = window;
            _maxCount = maxCount;
            _windowStart = DateTime.UtcNow;
        }

        public bool TryConsume(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _windowStart >= _window)
                {
                    _windowStart = now;
                    _count = 0;
                }

                if (_count + amount > _maxCount)
                {
                    return false;
                }

                _count += amount;
                return true;
            }
        }
    }

    private sealed class TranslationCache
    {
        private const string CacheFileName = "translation-cache.db";
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _initialized;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (_initialized)
            {
                return;
            }

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                var directory = Path.Combine(ServerContext.ResourceDirectory, "localization");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, CacheFileName);
                await using var connection = new SqliteConnection($"Data Source={path}");
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE IF NOT EXISTS translation_cache (
                        cache_key TEXT PRIMARY KEY,
                        translation TEXT NOT NULL,
                        updated_utc TEXT NOT NULL
                    );
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                _initialized = true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> FetchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken
        )
        {
            if (keys.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var directory = Path.Combine(ServerContext.ResourceDirectory, "localization");
                var path = Path.Combine(directory, CacheFileName);
                await using var connection = new SqliteConnection($"Data Source={path}");
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var command = connection.CreateCommand();
                var parameters = new List<string>(keys.Count);
                for (var i = 0; i < keys.Count; i++)
                {
                    var parameterName = $"$key{i}";
                    command.Parameters.AddWithValue(parameterName, keys[i]);
                    parameters.Add(parameterName);
                }

                command.CommandText = $"SELECT cache_key, translation FROM translation_cache WHERE cache_key IN ({string.Join(", ", parameters)});";
                var results = new Dictionary<string, string>(keys.Count);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results[reader.GetString(0)] = reader.GetString(1);
                }

                return results;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task StoreAsync(
            IReadOnlyDictionary<string, string> translations,
            CancellationToken cancellationToken
        )
        {
            if (translations.Count == 0)
            {
                return;
            }

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var directory = Path.Combine(ServerContext.ResourceDirectory, "localization");
                var path = Path.Combine(directory, CacheFileName);
                await using var connection = new SqliteConnection($"Data Source={path}");
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO translation_cache (cache_key, translation, updated_utc)
                    VALUES ($key, $translation, $updated)
                    ON CONFLICT(cache_key) DO UPDATE SET
                        translation = excluded.translation,
                        updated_utc = excluded.updated_utc;
                    """;
                var keyParameter = command.Parameters.Add("$key", SqliteType.Text);
                var translationParameter = command.Parameters.Add("$translation", SqliteType.Text);
                var updatedParameter = command.Parameters.Add("$updated", SqliteType.Text);
                foreach (var pair in translations)
                {
                    keyParameter.Value = pair.Key;
                    translationParameter.Value = pair.Value;
                    updatedParameter.Value = DateTime.UtcNow.ToString("O");
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
