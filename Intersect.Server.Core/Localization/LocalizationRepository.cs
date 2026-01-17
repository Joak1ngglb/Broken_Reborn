using System;
using System.Collections.Generic;
using System.IO;
using Intersect.Server.Core;
using Microsoft.Data.Sqlite;

namespace Intersect.Server.Localization;

public sealed class LocalizationRepository
{
    private const string DefaultLanguage = "en";
    private readonly string _databasePath;

    private static LocalizationRepository? _default;

    public static LocalizationRepository Default => _default ??= new LocalizationRepository(DefaultDatabasePath);

    public static string DefaultDatabasePath => Path.Combine(ServerContext.ResourceDirectory, "translations.db");

    public LocalizationRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public static void InitializeDefault()
    {
        Default.EnsureSchema();
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
            VALUES ($entityType, $entityId, $field, $lang, $text, $sourceHash, $updatedUtc)
            ON CONFLICT(entity_type, entity_id, field, lang)
            DO UPDATE SET
                text = excluded.text,
                source_hash = excluded.source_hash,
                updated_utc = excluded.updated_utc;
            """;
        command.Parameters.AddWithValue("$entityType", entityType);
        command.Parameters.AddWithValue("$entityId", entityId);
        command.Parameters.AddWithValue("$field", field);
        command.Parameters.AddWithValue("$lang", NormalizeLanguage(language));
        command.Parameters.AddWithValue("$text", text);
        command.Parameters.AddWithValue("$sourceHash", sourceHash);
        command.Parameters.AddWithValue("$updatedUtc", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    public string? Get(string entityType, string entityId, string field, string language)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT text
            FROM translations
            WHERE entity_type = $entityType
              AND entity_id = $entityId
              AND field = $field
              AND lang IN ($language, $fallback)
            ORDER BY CASE WHEN lang = $language THEN 0 ELSE 1 END
            LIMIT 1;
            """;
        var normalizedLanguage = NormalizeLanguage(language);
        command.Parameters.AddWithValue("$entityType", entityType);
        command.Parameters.AddWithValue("$entityId", entityId);
        command.Parameters.AddWithValue("$field", field);
        command.Parameters.AddWithValue("$language", normalizedLanguage);
        command.Parameters.AddWithValue("$fallback", DefaultLanguage);
        var result = command.ExecuteScalar();
        return result == DBNull.Value ? null : result as string;
    }

    public Dictionary<LocalizationKey, string> GetBatch(IEnumerable<LocalizationKey> keys, string language)
    {
        var results = new Dictionary<LocalizationKey, string>();
        foreach (var key in keys)
        {
            var text = Get(key.EntityType, key.EntityId, key.Field, language);
            if (!string.IsNullOrWhiteSpace(text))
            {
                results[key] = text;
            }
        }

        return results;
    }

    private SqliteConnection OpenConnection()
    {
        EnsureDatabaseExists();
        var connection = new SqliteConnection($"Data Source={_databasePath}");
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

public readonly record struct LocalizationKey(string EntityType, string EntityId, string Field);
