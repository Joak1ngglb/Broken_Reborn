using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Events.Commands;
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
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO translations (entity_type, entity_id, field, lang, text, source_hash, updated_utc)
            VALUES (@entityType, @entityId, @field, @lang, @text, @sourceHash, @updatedUtc)
            ON CONFLICT(entity_type, entity_id, field, lang)
            DO UPDATE SET
                text = excluded.text,
                source_hash = excluded.source_hash,
                updated_utc = excluded.updated_utc;
            """;
        command.Parameters.Add(new SqliteParameter("@entityType", entityType));
        command.Parameters.Add(new SqliteParameter("@entityId", entityId));
        command.Parameters.Add(new SqliteParameter("@field", field));
        command.Parameters.Add(new SqliteParameter("@lang", NormalizeLanguage(language)));
        command.Parameters.Add(new SqliteParameter("@text", text));
        command.Parameters.Add(new SqliteParameter("@sourceHash", sourceHash));
        command.Parameters.Add(new SqliteParameter("@updatedUtc", DateTime.UtcNow.ToString("O")));
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath},Version=3");
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL;";
        pragma.ExecuteNonQuery();
        return connection;
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
    private static readonly object SchemaLock = new();
    private static bool _schemaEnsured;

    public static void UpdateEnglishSource(string entityType, Guid entityId, string field, string text)
    {
        EnsureSchema();
        var normalizedText = text ?? string.Empty;
        var sourceHash = ComputeHash(normalizedText);
        TranslationRepository.Default.Upsert(
            entityType,
            entityId.ToString(),
            field,
            "en",
            normalizedText,
            sourceHash
        );
    }

    public static void UpdateEventEnglishSources(EventDescriptor eventDescriptor)
    {
        if (eventDescriptor == null)
        {
            return;
        }

        var entityType = eventDescriptor.Type.ToString();
        var entityId = eventDescriptor.Id;

        if (!string.IsNullOrWhiteSpace(eventDescriptor.Name))
        {
            UpdateEnglishSource(entityType, entityId, "Name", eventDescriptor.Name);
        }

        if (eventDescriptor.Pages == null)
        {
            return;
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
                UpdateEnglishSource(entityType, entityId, $"Page:{pageIndex}:Description", page.Description);
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
                            UpdateEnglishSource(
                                entityType,
                                entityId,
                                $"{baseField}:ShowText",
                                showTextCommand.Text
                            );
                            break;
                        case AddChatboxTextCommand addChatboxTextCommand:
                            UpdateEnglishSource(
                                entityType,
                                entityId,
                                $"{baseField}:ChatboxText",
                                addChatboxTextCommand.Text
                            );
                            break;
                        case ShowOptionsCommand showOptionsCommand:
                            UpdateEnglishSource(
                                entityType,
                                entityId,
                                $"{baseField}:OptionsText",
                                showOptionsCommand.Text
                            );
                            if (showOptionsCommand.Options != null)
                            {
                                for (var optionIndex = 0; optionIndex < showOptionsCommand.Options.Length; optionIndex++)
                                {
                                    UpdateEnglishSource(
                                        entityType,
                                        entityId,
                                        $"{baseField}:Option:{optionIndex}",
                                        showOptionsCommand.Options[optionIndex] ?? string.Empty
                                    );
                                }
                            }
                            break;
                        case InputVariableCommand inputVariableCommand:
                            UpdateEnglishSource(
                                entityType,
                                entityId,
                                $"{baseField}:InputTitle",
                                inputVariableCommand.Title ?? string.Empty
                            );
                            UpdateEnglishSource(
                                entityType,
                                entityId,
                                $"{baseField}:InputText",
                                inputVariableCommand.Text
                            );
                            break;
                        case ChangePlayerLabelCommand changePlayerLabelCommand:
                            UpdateEnglishSource(
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
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            if (_schemaEnsured)
            {
                return;
            }

            TranslationRepository.Default.EnsureSchema();
            _schemaEnsured = true;
        }
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
