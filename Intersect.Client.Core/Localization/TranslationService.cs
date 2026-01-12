using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intersect.Client.Networking;
using Intersect.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.Framework.Core.GameObjects.Spells;
using Intersect.Framework.Threading;
using Intersect.Network.Packets;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Client.Localization;

public class TranslationService
{
    private const int BatchSize = 20;
    private const int BatchTimeoutSeconds = 30;
    private const string TranslationScope = "client";

    private static TranslationService _instance;
    public static TranslationService Instance => _instance ??= new TranslationService();

    private readonly ConcurrentDictionary<string, string> _translationCache;
    private readonly ConcurrentDictionary<string, string> _translationKeyCache;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingTranslations;
    private readonly string _targetLanguage;
    private readonly string _cacheFilePath;
    private bool _enabled;

    private TranslationService()
    {
        _translationCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        _translationKeyCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        _pendingTranslations = new ConcurrentDictionary<string, TaskCompletionSource<string>>(StringComparer.Ordinal);

        var currentCulture = CultureInfo.CurrentUICulture;
        _targetLanguage = currentCulture.TwoLetterISOLanguageName;

        var cacheDir = Path.Combine("resources", "localization", "cache");
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }

        var sanitizedLang = string.Join("_", _targetLanguage.Split(Path.GetInvalidFileNameChars()));
        _cacheFilePath = Path.Combine(cacheDir, $"{sanitizedLang}.json");

        LoadCache();

        _enabled = !currentCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);
    }

    public static void Init()
    {
        if (Instance._enabled)
        {
            Task.Run(TranslateInterface);
        }
    }

    public async Task<string> Translate(string text)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (_translationCache.TryGetValue(text, out var cached))
        {
            return cached;
        }

        var request = new Dictionary<string, string> { { text, text } };
        var translated = await RequestTranslationBatch(request).ConfigureAwait(false);
        if (translated.TryGetValue(text, out var result))
        {
            _translationCache[text] = result;
            _translationKeyCache.TryAdd(text, result);
            SaveCache();
            return result;
        }

        return text;
    }

    public async Task<Dictionary<string, string>> TranslateBatch(
        Dictionary<string, string> inputs,
        Action<Dictionary<string, string>>? onChunkComplete = null
    )
    {
        var results = new Dictionary<string, string>();
        if (!_enabled || inputs.Count == 0)
        {
            return results;
        }

        var uncached = new Dictionary<string, string>();
        var cachedResults = new Dictionary<string, string>();

        foreach (var kvp in inputs)
        {
            if (_translationKeyCache.TryGetValue(kvp.Key, out var keyCached))
            {
                results[kvp.Key] = keyCached;
                cachedResults[kvp.Key] = keyCached;
            }
            else if (_translationCache.TryGetValue(kvp.Value, out var textCached))
            {
                results[kvp.Key] = textCached;
                cachedResults[kvp.Key] = textCached;
                _translationKeyCache.TryAdd(kvp.Key, textCached);
            }
            else
            {
                uncached.Add(kvp.Key, kvp.Value);
            }
        }

        if (cachedResults.Count > 0)
        {
            onChunkComplete?.Invoke(cachedResults);
        }

        if (uncached.Count == 0)
        {
            return results;
        }

        ApplicationContext.Context.Value?.Logger.LogInformation(
            "Starting translation of {Count} strings to {Language}...",
            uncached.Count,
            _targetLanguage
        );

        var chunks = uncached.Select(x => x).Chunk(BatchSize);

        foreach (var chunk in chunks)
        {
            var chunkDict = chunk.ToDictionary(k => k.Key, v => v.Value);
            var chunkResults = new Dictionary<string, string>();

            try
            {
                var translatedChunk = await RequestTranslationBatch(chunkDict).ConfigureAwait(false);
                foreach (var kvp in translatedChunk)
                {
                    results[kvp.Key] = kvp.Value;
                    chunkResults[kvp.Key] = kvp.Value;

                    if (chunkDict.TryGetValue(kvp.Key, out var originalText))
                    {
                        _translationCache[originalText] = kvp.Value;
                        _translationKeyCache[kvp.Key] = kvp.Value;
                    }
                }

                SaveCache();

                ApplicationContext.Context.Value?.Logger.LogInformation(
                    "Translated batch of {Count} strings.",
                    chunkDict.Count
                );

                if (chunkResults.Count > 0)
                {
                    onChunkComplete?.Invoke(chunkResults);
                }
            }
            catch (Exception ex)
            {
                ApplicationContext.Context.Value?.Logger.LogError(ex, "Failed to translate batch");
            }
        }

        return results;
    }

    public void UpdateFromResponse(TranslationBatchResponsePacket packet)
    {
        if (packet?.Entries == null || packet.Entries.Length == 0)
        {
            return;
        }

        foreach (var entry in packet.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                continue;
            }

            var translation = entry.Text ?? string.Empty;
            _translationKeyCache[entry.Id] = translation;

            if (_pendingTranslations.TryRemove(entry.Id, out var completionSource))
            {
                completionSource.TrySetResult(translation);
            }
        }
    }

    private async Task<Dictionary<string, string>> RequestTranslationBatch(Dictionary<string, string> texts)
    {
        var results = new Dictionary<string, string>();
        if (texts.Count == 0)
        {
            return results;
        }

        var entries = texts.Select(kvp => new TranslationBatchEntry(kvp.Key, kvp.Value)).ToArray();
        var keys = new List<string>(entries.Length);
        var tasks = new List<Task<string>>(entries.Length);

        foreach (var entry in entries)
        {
            keys.Add(entry.Id);
            var completionSource = _pendingTranslations.GetOrAdd(
                entry.Id,
                _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously)
            );
            tasks.Add(completionSource.Task);
        }

        PacketSender.SendTranslationBatch(_targetLanguage, null, TranslationScope, entries);

        var allTask = Task.WhenAll(tasks);
        var completed = await Task.WhenAny(allTask, Task.Delay(TimeSpan.FromSeconds(BatchTimeoutSeconds))).ConfigureAwait(false);
        if (completed != allTask)
        {
            ApplicationContext.Context.Value?.Logger.LogWarning("Translation batch timed out.");
            foreach (var key in keys)
            {
                _pendingTranslations.TryRemove(key, out _);
                if (texts.TryGetValue(key, out var fallback))
                {
                    results[key] = fallback;
                }
            }

            return results;
        }

        var translations = await allTask.ConfigureAwait(false);
        for (var i = 0; i < keys.Count; i++)
        {
            results[keys[i]] = translations[i];
        }

        return results;
    }

    private void LoadCache()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return;
            }

            var json = File.ReadAllText(_cacheFilePath, Encoding.UTF8);
            var loadedCache = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (loadedCache == null)
            {
                return;
            }

            foreach (var kvp in loadedCache)
            {
                _translationKeyCache.TryAdd(kvp.Key, kvp.Value);

                var isStructureKey = kvp.Key.Contains('.') || kvp.Key.Contains('_');
                if (!isStructureKey)
                {
                    _translationCache.TryAdd(kvp.Key, kvp.Value);
                }
            }

            ApplicationContext.Context.Value?.Logger.LogInformation(
                "Loaded {Count} translations from cache.",
                _translationKeyCache.Count
            );
        }
        catch (Exception ex)
        {
            ApplicationContext.Context.Value?.Logger.LogError(ex, "Failed to load translation cache");
        }
    }

    private void SaveCache()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_translationKeyCache, Formatting.Indented);
            File.WriteAllText(_cacheFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            ApplicationContext.Context.Value?.Logger.LogError(ex, "Failed to save translation cache");
        }
    }

    private static Task TranslateInterface()
    {
        return Task.CompletedTask;
    }

    public static async Task TranslateGameContent()
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        var inputs = new Dictionary<string, string>();

        if (ItemDescriptor.Lookup != null)
        {
            foreach (var item in ItemDescriptor.Lookup.Values.OfType<ItemDescriptor>())
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    inputs[$"ITEM_NAME_{item.Id}"] = item.Name;
                }

                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    inputs[$"ITEM_DESC_{item.Id}"] = item.Description;
                }
            }
        }

        if (QuestDescriptor.Lookup != null)
        {
            foreach (var quest in QuestDescriptor.Lookup.Values.OfType<QuestDescriptor>())
            {
                if (!string.IsNullOrWhiteSpace(quest.Name))
                {
                    inputs[$"QUEST_NAME_{quest.Id}"] = quest.Name;
                }

                if (!string.IsNullOrWhiteSpace(quest.StartDescription))
                {
                    inputs[$"QUEST_START_{quest.Id}"] = quest.StartDescription;
                }

                if (!string.IsNullOrWhiteSpace(quest.BeforeDescription))
                {
                    inputs[$"QUEST_BEFORE_{quest.Id}"] = quest.BeforeDescription;
                }

                if (!string.IsNullOrWhiteSpace(quest.InProgressDescription))
                {
                    inputs[$"QUEST_INPROG_{quest.Id}"] = quest.InProgressDescription;
                }

                if (!string.IsNullOrWhiteSpace(quest.EndDescription))
                {
                    inputs[$"QUEST_END_{quest.Id}"] = quest.EndDescription;
                }

                if (quest.Tasks == null)
                {
                    continue;
                }

                foreach (var task in quest.Tasks)
                {
                    if (!string.IsNullOrWhiteSpace(task.Description))
                    {
                        inputs[$"QUEST_TASK_{quest.Id}_{task.Id}"] = task.Description;
                    }
                }
            }
        }

        if (SpellDescriptor.Lookup != null)
        {
            foreach (var spell in SpellDescriptor.Lookup.Values.OfType<SpellDescriptor>())
            {
                if (!string.IsNullOrWhiteSpace(spell.Name))
                {
                    inputs[$"SPELL_NAME_{spell.Id}"] = spell.Name;
                }

                if (!string.IsNullOrWhiteSpace(spell.Description))
                {
                    inputs[$"SPELL_DESC_{spell.Id}"] = spell.Description;
                }
            }
        }

        if (inputs.Count > 0)
        {
            ApplicationContext.Context.Value?.Logger.LogInformation(
                "Queuing {Count} game strings for translation...",
                inputs.Count
            );
        }

        await Instance.TranslateBatch(inputs, chunkResults =>
        {
            ThreadQueue.Default.RunOnMainThread(() =>
            {
                foreach (var kvp in chunkResults)
                {
                    var key = kvp.Key;
                    var translatedText = kvp.Value;
                    var parts = key.Split('_');

                    if (parts.Length < 3)
                    {
                        continue;
                    }

                    var type = parts[0];
                    var field = parts[1];

                    if (!Guid.TryParse(parts[2], out var id))
                    {
                        continue;
                    }

                    if (type == "ITEM")
                    {
                        if (ItemDescriptor.TryGet(id, out var item))
                        {
                            if (field == "NAME")
                            {
                                item.Name = translatedText;
                            }
                            else if (field == "DESC")
                            {
                                item.Description = translatedText;
                            }
                        }
                    }
                    else if (type == "QUEST")
                    {
                        if (QuestDescriptor.TryGet(id, out var quest))
                        {
                            if (field == "NAME")
                            {
                                quest.Name = translatedText;
                            }
                            else if (field == "START")
                            {
                                quest.StartDescription = translatedText;
                            }
                            else if (field == "BEFORE")
                            {
                                quest.BeforeDescription = translatedText;
                            }
                            else if (field == "INPROG")
                            {
                                quest.InProgressDescription = translatedText;
                            }
                            else if (field == "END")
                            {
                                quest.EndDescription = translatedText;
                            }
                            else if (field == "TASK" &&
                                     parts.Length >= 4 &&
                                     Guid.TryParse(parts[3], out var taskId))
                            {
                                var task = quest.FindTask(taskId);
                                if (task != null)
                                {
                                    task.Description = translatedText;
                                }
                            }
                        }
                    }
                    else if (type == "SPELL")
                    {
                        if (SpellDescriptor.TryGet(id, out var spell))
                        {
                            if (field == "NAME")
                            {
                                spell.Name = translatedText;
                            }
                            else if (field == "DESC")
                            {
                                spell.Description = translatedText;
                            }
                        }
                    }
                }

                if (Intersect.Client.Interface.Interface.HasInGameUI)
                {
                    Intersect.Client.Interface.Interface.GameUi.NotifyQuestsUpdated();
                }
            });
        }).ConfigureAwait(false);
    }
}
