using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Intersect.Editor.Networking;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Events.Commands;
using Intersect.Framework.Core.Localization;
using Intersect.GameObjects;
using Intersect.Network.Packets.Editor;
using Mono.Data.Sqlite;

namespace Intersect.Editor.Localization;

public sealed class TranslationRepository
{
    private const string DefaultLanguage = "en";
    private readonly string _databasePath;

    private static TranslationRepository? _default;

    public static TranslationRepository Default => _default ??= new TranslationRepository(DefaultDatabasePath);

    public static string DefaultDatabasePath => Path.Combine("resources", "translations.db");

    public TranslationRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public void EnsureSchema()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS translations (
                entity_type TEXT NOT NULL,
                entity_id TEXT NOT NULL,
                field TEXT NOT NULL,
                lang TEXT NOT NULL,
                text TEXT NOT NULL,
                source_hash TEXT NOT NULL,
                updated_utc TEXT NOT NULL,
                PRIMARY KEY (entity_type, entity_id, field, lang)
            );

            CREATE INDEX IF NOT EXISTS idx_translations_identity
                ON translations (entity_type, entity_id, field, lang);

            CREATE INDEX IF NOT EXISTS idx_translations_lang
                ON translations (lang);
            """;
        command.ExecuteNonQuery();
    }

    public void Upsert(
        string entityType,
        string entityId,
        string field,
        string language,
        string text,
        string sourceHash
    )
    {
        using var connection = OpenConnection();
        var normalizedLanguage = NormalizeLanguage(language);
        var updatedUtc = DateTime.UtcNow.ToString("O");
        using var update = connection.CreateCommand();
        update.CommandText =
            """
            UPDATE translations
            SET text = @text,
                source_hash = @sourceHash,
                updated_utc = @updatedUtc
            WHERE entity_type = @entityType
              AND entity_id = @entityId
              AND field = @field
              AND lang = @lang;
            """;
        update.Parameters.Add(new SqliteParameter("@text", text));
        update.Parameters.Add(new SqliteParameter("@sourceHash", sourceHash));
        update.Parameters.Add(new SqliteParameter("@updatedUtc", updatedUtc));
        update.Parameters.Add(new SqliteParameter("@entityType", entityType));
        update.Parameters.Add(new SqliteParameter("@entityId", entityId));
        update.Parameters.Add(new SqliteParameter("@field", field));
        update.Parameters.Add(new SqliteParameter("@lang", normalizedLanguage));
        if (update.ExecuteNonQuery() > 0)
        {
            return;
        }

        using var insert = connection.CreateCommand();
        insert.CommandText =
            """
            INSERT INTO translations (entity_type, entity_id, field, lang, text, source_hash, updated_utc)
            VALUES (@entityType, @entityId, @field, @lang, @text, @sourceHash, @updatedUtc);
            """;
        insert.Parameters.Add(new SqliteParameter("@entityType", entityType));
        insert.Parameters.Add(new SqliteParameter("@entityId", entityId));
        insert.Parameters.Add(new SqliteParameter("@field", field));
        insert.Parameters.Add(new SqliteParameter("@lang", normalizedLanguage));
        insert.Parameters.Add(new SqliteParameter("@text", text));
        insert.Parameters.Add(new SqliteParameter("@sourceHash", sourceHash));
        insert.Parameters.Add(new SqliteParameter("@updatedUtc", updatedUtc));
        insert.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        EnsureDatabaseExists();
        var connection = new SqliteConnection($"Data Source={_databasePath},Version=3");
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    private void EnsureDatabaseExists()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_databasePath))
        {
            using var _ = File.Create(_databasePath);
        }
    }

    private static string NormalizeLanguage(string language)
    {
        return string.IsNullOrWhiteSpace(language)
            ? DefaultLanguage
            : language.Trim().ToLowerInvariant();
    }
}

public static class TranslationSourceUpdater
{
    private const string DefaultLanguage = "en";
    private const int DefaultBatchSize = 200;

    // Status: 0 OK, 1 NEEDS_REVIEW, 2 MISSING, 3 MACHINE

    public static TranslationUpsertEntry CreateSourceEntry(
        string entityType,
        Guid entityId,
        string field,
        string sourceText
    )
    {
        return new TranslationUpsertEntry(
            entityType,
            entityId.ToString(),
            field,
            sourceText ?? string.Empty,   // SourceText
            DefaultLanguage,              // Language (no importa mucho si no hay TranslatedText)
            string.Empty,                 // TranslatedText vacío = solo source
            TranslationStatus.Missing
        );
    }

    public static void AddSource(
        ICollection<TranslationUpsertEntry> entries,
        string entityType,
        Guid entityId,
        string field,
        string sourceText
    )
    {
        if (entries == null) return;
        entries.Add(CreateSourceEntry(entityType, entityId, field, sourceText));
    }

    public static void QueueBatchSources(IEnumerable<TranslationUpsertEntry> entries, int batchSize = DefaultBatchSize)
    {
        if (entries == null) return;

        var entryList = entries as IReadOnlyCollection<TranslationUpsertEntry> ?? new List<TranslationUpsertEntry>(entries);
        if (entryList.Count == 0) return;

        Task.Run(() => SendBatchSources(entryList, batchSize));
    }

    public static void UpdateSource(string entityType, Guid entityId, string field, string sourceText)
    {
        var entry = CreateSourceEntry(entityType, entityId, field, sourceText);
        PacketSender.SendTranslationBatchUpsert(new List<TranslationUpsertEntry> { entry });
    }

    public static void UpdateEventSources(EventDescriptor eventDescriptor)
    {
        var entries = GetEventSources(eventDescriptor);
        QueueBatchSources(entries);
    }

    public static IReadOnlyList<TranslationUpsertEntry> GetEventSources(EventDescriptor eventDescriptor)
    {
        var entries = new List<TranslationUpsertEntry>();
        if (eventDescriptor == null) return entries;

        var entityType = LocalizationEntityTypes.Event;
        var entityId = eventDescriptor.Id;

        if (!string.IsNullOrWhiteSpace(eventDescriptor.Name))
        {
            AddSource(entries, entityType, entityId, LocalizationFields.Name, eventDescriptor.Name);
        }


        if (eventDescriptor.Pages == null) return entries;

        for (var pageIndex = 0; pageIndex < eventDescriptor.Pages.Count; pageIndex++)
        {
            var page = eventDescriptor.Pages[pageIndex];
            if (page == null) continue;

            if (!string.IsNullOrWhiteSpace(page.Description))
            {
                AddSource(entries, entityType, entityId, EventFieldKey.PageDescription(pageIndex), page.Description);
            }

            if (page.CommandLists == null) continue;

            foreach (var (listId, commands) in page.CommandLists)
            {
                if (commands == null) continue;

                for (var commandIndex = 0; commandIndex < commands.Count; commandIndex++)
                {
                    var command = commands[commandIndex];
                    if (command == null) continue;

                    switch (command)
                    {
                        case ShowTextCommand showTextCommand:
                            AddSource(entries, entityType, entityId, EventFieldKey.ShowText(pageIndex, listId, commandIndex), showTextCommand.Text);
                            break;

                        case AddChatboxTextCommand addChatboxTextCommand:
                            AddSource(entries, entityType, entityId, EventFieldKey.ChatboxText(pageIndex, listId, commandIndex), addChatboxTextCommand.Text);
                            break;

                        case ShowOptionsCommand showOptionsCommand:
                            AddSource(entries, entityType, entityId, EventFieldKey.OptionsText(pageIndex, listId, commandIndex), showOptionsCommand.Text);
                            if (showOptionsCommand.Options != null)
                            {
                                for (var optionIndex = 0; optionIndex < showOptionsCommand.Options.Length; optionIndex++)
                                {
                                    AddSource(
                                        entries,
                                        entityType,
                                        entityId,
                                        EventFieldKey.Option(pageIndex, listId, commandIndex, optionIndex),
                                        showOptionsCommand.Options[optionIndex] ?? string.Empty
                                    );
                                }
                            }
                            break;

                        case InputVariableCommand inputVariableCommand:
                            AddSource(entries, entityType, entityId, EventFieldKey.InputTitle(pageIndex, listId, commandIndex), inputVariableCommand.Title ?? string.Empty);
                            AddSource(entries, entityType, entityId, EventFieldKey.InputText(pageIndex, listId, commandIndex), inputVariableCommand.Text);
                            break;

                        case ChangePlayerLabelCommand changePlayerLabelCommand:
                            AddSource(entries, entityType, entityId, EventFieldKey.PlayerLabel(pageIndex, listId, commandIndex), changePlayerLabelCommand.Value ?? string.Empty);
                            break;
                    }
                }
            }
        }

        return entries;
    }

    public static IReadOnlyList<TranslationUpsertEntry> GetQuestAndRelatedEventSources(QuestDescriptor quest)
    {
        var entries = new List<TranslationUpsertEntry>();
        if (quest == null)
        {
            return entries;
        }

        var entityType = quest.Type.ToString();
        var entityId = quest.Id;

        AddSource(entries, entityType, entityId, QuestFieldKey.Name, quest.Name);
        AddSource(entries, entityType, entityId, QuestFieldKey.BeforeDescription, quest.BeforeDescription);
        AddSource(entries, entityType, entityId, QuestFieldKey.StartDescription, quest.StartDescription);
        AddSource(entries, entityType, entityId, QuestFieldKey.InProgressDescription, quest.InProgressDescription);
        AddSource(entries, entityType, entityId, QuestFieldKey.EndDescription, quest.EndDescription);

        var relatedEventIds = new HashSet<Guid>();
        AddEventEntries(entries, quest.StartEvent, relatedEventIds);
        AddEventEntries(entries, quest.EndEvent, relatedEventIds);

        if (quest.Tasks == null)
        {
            return entries;
        }

        foreach (var task in quest.Tasks)
        {
            if (task == null)
            {
                continue;
            }

            AddSource(entries, entityType, entityId, QuestFieldKey.TaskDescription(task.Id), task.Description);

            AddEventEntries(entries, task.CompletionEvent, relatedEventIds);
            AddEventEntries(entries, task.EditingEvent, relatedEventIds);
        }

        return entries;
    }

    private static void AddEventEntries(
        ICollection<TranslationUpsertEntry> entries,
        EventDescriptor eventDescriptor,
        ISet<Guid> processedEventIds
    )
    {
        if (eventDescriptor == null || eventDescriptor.Id == Guid.Empty)
        {
            return;
        }

        if (!processedEventIds.Add(eventDescriptor.Id))
        {
            return;
        }

        foreach (var eventEntry in GetEventSources(eventDescriptor))
        {
            entries.Add(eventEntry);
        }
    }

    private static void SendBatchSources(IEnumerable<TranslationUpsertEntry> entries, int batchSize)
    {
        var batch = new List<TranslationUpsertEntry>(batchSize);

        foreach (var entry in entries)
        {
            if (entry == null) continue;

            batch.Add(entry);
            if (batch.Count < batchSize) continue;

            PacketSender.SendTranslationBatchUpsert(batch);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            PacketSender.SendTranslationBatchUpsert(batch);
        }
    }
    // --- Backward compatible wrappers (no rompen frmEvent.cs) ---

    public static void UpdateEventEnglishSources(EventDescriptor eventDescriptor)
    {
        UpdateEventSources(eventDescriptor);
    }

    public static IReadOnlyList<TranslationUpsertEntry> GetEventEnglishSources(EventDescriptor eventDescriptor)
    {
        return GetEventSources(eventDescriptor);
    }

    public static void QueueBatchEnglishSources(IEnumerable<TranslationUpsertEntry> entries, int batchSize = DefaultBatchSize)
    {
        QueueBatchSources(entries, batchSize);
    }

    public static void UpdateEnglishSource(string entityType, Guid entityId, string field, string sourceText)
    {
        UpdateSource(entityType, entityId, field, sourceText);
    }

    public static void AddEnglishSource(
        ICollection<TranslationUpsertEntry> entries,
        string entityType,
        Guid entityId,
        string field,
        string sourceText
    )
    {
        AddSource(entries, entityType, entityId, field, sourceText);
    }
}
