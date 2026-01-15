using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Intersect.Editor.Networking;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Events.Commands;
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

    public static TranslationUpsertEntry CreateEnglishSourceEntry(
        string entityType,
        Guid entityId,
        string field,
        string text
    )
    {
        var normalizedText = text ?? string.Empty;
        return new TranslationUpsertEntry(
            entityType,
            entityId.ToString(),
            field,
            DefaultLanguage,
            normalizedText,
            ComputeHash(normalizedText)
        );
    }

    public static void AddEnglishSource(
        ICollection<TranslationUpsertEntry> entries,
        string entityType,
        Guid entityId,
        string field,
        string text
    )
    {
        if (entries == null)
        {
            return;
        }

        entries.Add(CreateEnglishSourceEntry(entityType, entityId, field, text));
    }

    public static void QueueBatchEnglishSources(
        IEnumerable<TranslationUpsertEntry> entries,
        int batchSize = DefaultBatchSize
    )
    {
        if (entries == null)
        {
            return;
        }

        var entryList = entries as IReadOnlyCollection<TranslationUpsertEntry> ?? new List<TranslationUpsertEntry>(entries);
        if (entryList.Count == 0)
        {
            return;
        }

        Task.Run(() => SendBatchEnglishSources(entryList, batchSize));
    }

    public static void UpdateEnglishSource(string entityType, Guid entityId, string field, string text)
    {
        var entry = CreateEnglishSourceEntry(entityType, entityId, field, text);
        PacketSender.SendTranslationUpsert(
            entry.EntityType,
            entry.EntityId,
            entry.Field,
            entry.Language,
            entry.Text,
            entry.SourceHash
        );
    }

    public static void UpdateEventEnglishSources(EventDescriptor eventDescriptor)
    {
        var entries = GetEventEnglishSources(eventDescriptor);
        foreach (var entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            PacketSender.SendTranslationUpsert(
                entry.EntityType,
                entry.EntityId,
                entry.Field,
                entry.Language,
                entry.Text,
                entry.SourceHash
            );
        }
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    public static IReadOnlyList<TranslationUpsertEntry> GetEventEnglishSources(EventDescriptor eventDescriptor)
    {
        var entries = new List<TranslationUpsertEntry>();
        if (eventDescriptor == null)
        {
            return entries;
        }

        var entityType = eventDescriptor.Type.ToString();
        var entityId = eventDescriptor.Id;

        if (!string.IsNullOrWhiteSpace(eventDescriptor.Name))
        {
            AddEnglishSource(entries, entityType, entityId, "Name", eventDescriptor.Name);
        }

        if (eventDescriptor.Pages == null)
        {
            return entries;
        }

        for (var pageIndex = 0; pageIndex < eventDescriptor.Pages.Count; pageIndex++)
        {
            var page = eventDescriptor.Pages[pageIndex];
            if (page == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(page.Description))
            {
                AddEnglishSource(entries, entityType, entityId, $"Page:{pageIndex}:Description", page.Description);
            }

            if (page.CommandLists == null)
            {
                continue;
            }

            foreach (var (listId, commands) in page.CommandLists)
            {
                if (commands == null)
                {
                    continue;
                }

                for (var commandIndex = 0; commandIndex < commands.Count; commandIndex++)
                {
                    var command = commands[commandIndex];
                    if (command == null)
                    {
                        continue;
                    }

                    var baseField = $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}";
                    switch (command)
                    {
                        case ShowTextCommand showTextCommand:
                            AddEnglishSource(
                                entries,
                                entityType,
                                entityId,
                                $"{baseField}:ShowText",
                                showTextCommand.Text
                            );
                            break;
                        case AddChatboxTextCommand addChatboxTextCommand:
                            AddEnglishSource(
                                entries,
                                entityType,
                                entityId,
                                $"{baseField}:ChatboxText",
                                addChatboxTextCommand.Text
                            );
                            break;
                        case ShowOptionsCommand showOptionsCommand:
                            AddEnglishSource(
                                entries,
                                entityType,
                                entityId,
                                $"{baseField}:OptionsText",
                                showOptionsCommand.Text
                            );
                            if (showOptionsCommand.Options != null)
                            {
                                for (var optionIndex = 0; optionIndex < showOptionsCommand.Options.Length; optionIndex++)
                                {
                                    AddEnglishSource(
                                        entries,
                                        entityType,
                                        entityId,
                                        $"{baseField}:Option:{optionIndex}",
                                        showOptionsCommand.Options[optionIndex] ?? string.Empty
                                    );
                                }
                            }
                            break;
                        case InputVariableCommand inputVariableCommand:
                            AddEnglishSource(
                                entries,
                                entityType,
                                entityId,
                                $"{baseField}:InputTitle",
                                inputVariableCommand.Title ?? string.Empty
                            );
                            AddEnglishSource(
                                entries,
                                entityType,
                                entityId,
                                $"{baseField}:InputText",
                                inputVariableCommand.Text
                            );
                            break;
                        case ChangePlayerLabelCommand changePlayerLabelCommand:
                            AddEnglishSource(
                                entries,
                                entityType,
                                entityId,
                                $"{baseField}:PlayerLabel",
                                changePlayerLabelCommand.Value ?? string.Empty
                            );
                            break;
                    }
                }
            }
        }

        return entries;
    }

    private static void SendBatchEnglishSources(
        IEnumerable<TranslationUpsertEntry> entries,
        int batchSize
    )
    {
        var batch = new List<TranslationUpsertEntry>(batchSize);
        foreach (var entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            batch.Add(entry);
            if (batch.Count < batchSize)
            {
                continue;
            }

            PacketSender.SendTranslationBatchUpsert(batch);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            PacketSender.SendTranslationBatchUpsert(batch);
        }
    }
}
