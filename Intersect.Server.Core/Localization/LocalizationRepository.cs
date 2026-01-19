using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Intersect.Framework.Core.Localization;
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
        Default.MigrateLegacySchemaIfNeeded();
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
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS localization_source (
                entity_type TEXT NOT NULL,
                entity_id   TEXT NOT NULL,
                field       TEXT NOT NULL,

                source_text TEXT NOT NULL,
                source_hash TEXT NOT NULL,
                updated_utc TEXT NOT NULL,

                PRIMARY KEY (entity_type, entity_id, field)
            );

            CREATE INDEX IF NOT EXISTS idx_localization_source_hash
                ON localization_source (source_hash);

            CREATE TABLE IF NOT EXISTS localization_translation (
                entity_type TEXT NOT NULL,
                entity_id   TEXT NOT NULL,
                field       TEXT NOT NULL,
                lang        TEXT NOT NULL,

                source_hash     TEXT NOT NULL,
                translated_text TEXT NOT NULL,

                status      INTEGER NOT NULL,
                updated_utc TEXT NOT NULL,

                PRIMARY KEY (entity_type, entity_id, field, lang, source_hash),

                FOREIGN KEY (entity_type, entity_id, field)
                    REFERENCES localization_source (entity_type, entity_id, field)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_localization_translation_lookup
                ON localization_translation (entity_type, entity_id, field, lang);

            CREATE INDEX IF NOT EXISTS idx_localization_translation_lang
                ON localization_translation (lang);

            CREATE INDEX IF NOT EXISTS idx_localization_translation_status
                ON localization_translation (status);
            """;

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Upsert del texto fuente (lo que sale del editor). El hash se calcula EN SERVER.
    /// Esto es lo que hace que "si yo edito un texto" se actualice la base y marque NEEDS_REVIEW lo viejo.
    /// </summary>
    public string UpsertSource(string entityType, string entityId, string field, string sourceText)
    {
        entityType = RequireNotBlank(entityType, nameof(entityType));
        entityId = RequireNotBlank(entityId, nameof(entityId));
        field = RequireNotBlank(field, nameof(field));

        sourceText ??= string.Empty;
        var normalizedSource = NormalizeText(sourceText);
        var newHash = ComputeSha256Hex(normalizedSource);
        var now = DateTime.UtcNow.ToString("O");

        using var connection = OpenConnection();
        using var tx = connection.BeginTransaction();

        // 1) Leer hash actual
        string? oldHash = null;
        using (var select = connection.CreateCommand())
        {
            select.Transaction = tx;
            select.CommandText =
                """
                SELECT source_hash
                FROM localization_source
                WHERE entity_type = $entityType AND entity_id = $entityId AND field = $field
                LIMIT 1;
                """;
            select.Parameters.AddWithValue("$entityType", entityType);
            select.Parameters.AddWithValue("$entityId", entityId);
            select.Parameters.AddWithValue("$field", field);

            var scalar = select.ExecuteScalar();
            oldHash = scalar == DBNull.Value ? null : scalar as string;
        }

        // 2) Upsert source
        using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = tx;
            upsert.CommandText =
                """
                INSERT INTO localization_source (entity_type, entity_id, field, source_text, source_hash, updated_utc)
                VALUES ($entityType, $entityId, $field, $sourceText, $sourceHash, $updatedUtc)
                ON CONFLICT(entity_type, entity_id, field)
                DO UPDATE SET
                    source_text = excluded.source_text,
                    source_hash = excluded.source_hash,
                    updated_utc = excluded.updated_utc;
                """;
            upsert.Parameters.AddWithValue("$entityType", entityType);
            upsert.Parameters.AddWithValue("$entityId", entityId);
            upsert.Parameters.AddWithValue("$field", field);
            upsert.Parameters.AddWithValue("$sourceText", sourceText);
            upsert.Parameters.AddWithValue("$sourceHash", newHash);
            upsert.Parameters.AddWithValue("$updatedUtc", now);
            upsert.ExecuteNonQuery();
        }

        // 3) Si cambió el hash, marcamos NEEDS_REVIEW todas las traducciones del sourceKey que NO sean el hash actual
        if (!string.IsNullOrWhiteSpace(oldHash) && !string.Equals(oldHash, newHash, StringComparison.Ordinal))
        {
            using var needsReview = connection.CreateCommand();
            needsReview.Transaction = tx;
            needsReview.CommandText =
                """
                UPDATE localization_translation
                SET status = $stale, updated_utc = $updatedUtc
                WHERE entity_type = $entityType
                  AND entity_id = $entityId
                  AND field = $field
                  AND source_hash <> $newHash;
                """;
            needsReview.Parameters.AddWithValue("$stale", (int)TranslationStatus.NeedsReview);
            needsReview.Parameters.AddWithValue("$updatedUtc", now);
            needsReview.Parameters.AddWithValue("$entityType", entityType);
            needsReview.Parameters.AddWithValue("$entityId", entityId);
            needsReview.Parameters.AddWithValue("$field", field);
            needsReview.Parameters.AddWithValue("$newHash", newHash);
            needsReview.ExecuteNonQuery();
        }

        tx.Commit();
        return newHash;
    }

    /// <summary>
    /// Upsert de una traducción para la versión actual del source (o para un hash específico si lo pasas).
    /// Lo normal: llamas UpsertSource antes, y guardas traducción usando el hash retornado.
    /// </summary>
    public void UpsertTranslation(
        string entityType,
        string entityId,
        string field,
        string language,
        string translatedText,
        TranslationStatus status,
        string sourceHash
    )
    {
        entityType = RequireNotBlank(entityType, nameof(entityType));
        entityId = RequireNotBlank(entityId, nameof(entityId));
        field = RequireNotBlank(field, nameof(field));

        translatedText ??= string.Empty;

        var lang = NormalizeLanguage(language);
        var now = DateTime.UtcNow.ToString("O");

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO localization_translation (
                entity_type, entity_id, field, lang,
                source_hash, translated_text,
                status, updated_utc
            )
            VALUES (
                $entityType, $entityId, $field, $lang,
                $sourceHash, $translatedText,
                $status, $updatedUtc
            )
            ON CONFLICT(entity_type, entity_id, field, lang, source_hash)
            DO UPDATE SET
                translated_text = excluded.translated_text,
                status = excluded.status,
                updated_utc = excluded.updated_utc;
            """;

        command.Parameters.AddWithValue("$entityType", entityType);
        command.Parameters.AddWithValue("$entityId", entityId);
        command.Parameters.AddWithValue("$field", field);
        command.Parameters.AddWithValue("$lang", lang);
        command.Parameters.AddWithValue("$sourceHash", RequireNotBlank(sourceHash, nameof(sourceHash)));
        command.Parameters.AddWithValue("$translatedText", translatedText);
        command.Parameters.AddWithValue("$status", (int)status);
        command.Parameters.AddWithValue("$updatedUtc", now);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Obtiene el texto para el idioma elegido:
    /// - Busca el source actual
    /// - Busca traducción SOLO para el source_hash actual (y que no sea NEEDS_REVIEW)
    /// - Si no hay, fallback a DefaultLanguage
    /// - Si no hay, devuelve null (y tú haces fallback al original donde lo llames)
    /// </summary>
    public string? Get(string entityType, string entityId, string field, string language)
    {
        using var connection = OpenConnection();

        // 1) Source actual
        string? currentHash;
        using (var src = connection.CreateCommand())
        {
            src.CommandText =
                """
                SELECT source_hash
                FROM localization_source
                WHERE entity_type = $entityType
                  AND entity_id = $entityId
                  AND field = $field
                LIMIT 1;
                """;
            src.Parameters.AddWithValue("$entityType", entityType);
            src.Parameters.AddWithValue("$entityId", entityId);
            src.Parameters.AddWithValue("$field", field);

            var scalar = src.ExecuteScalar();
            currentHash = scalar == DBNull.Value ? null : scalar as string;
        }

        if (string.IsNullOrWhiteSpace(currentHash))
        {
            return null;
        }

        // 2) Traducción (idioma elegido -> fallback default), SOLO para hash actual y NO needs_review
        using var tr = connection.CreateCommand();
        tr.CommandText =
            """
            SELECT translated_text
            FROM localization_translation
            WHERE entity_type = $entityType
              AND entity_id = $entityId
              AND field = $field
              AND source_hash = $sourceHash
              AND status <> $stale
              AND lang IN ($language, $fallback)
            ORDER BY CASE WHEN lang = $language THEN 0 ELSE 1 END
            LIMIT 1;
            """;

        var normalizedLanguage = NormalizeLanguage(language);
        tr.Parameters.AddWithValue("$entityType", entityType);
        tr.Parameters.AddWithValue("$entityId", entityId);
        tr.Parameters.AddWithValue("$field", field);
        tr.Parameters.AddWithValue("$sourceHash", currentHash);
        tr.Parameters.AddWithValue("$stale", (int)TranslationStatus.NeedsReview);
        tr.Parameters.AddWithValue("$language", normalizedLanguage);
        tr.Parameters.AddWithValue("$fallback", DefaultLanguage);

        var result = tr.ExecuteScalar();
        return result == DBNull.Value ? null : result as string;
    }

    public Dictionary<LocalizationKey, string> GetBatch(IEnumerable<LocalizationKey> keys, string language)
    {
        // Mantengo simple como tú lo tienes (no optimizado con IN gigante)
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

    // --- Helpers / Migration ---

    private void MigrateLegacySchemaIfNeeded()
    {
        using var connection = OpenConnection();

        bool hasLegacy;
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                SELECT 1
                FROM sqlite_master
                WHERE type='table' AND name='translations'
                LIMIT 1;
                """;
            hasLegacy = cmd.ExecuteScalar() != null;
        }

        if (!hasLegacy)
        {
            return;
        }

        // Si existe legacy, copiamos:
        // - Creamos source con un placeholder del source_text (no lo tenías guardado antes),
        //   pero por lo menos mantenemos source_hash.
        // - Creamos translation con status OK.
        //
        // Importante: sin source_text real, tú igual puedes seguir funcionando,
        // pero lo ideal es que el editor empiece a llamar UpsertSource con el texto real y lo corrige.
        using var tx = connection.BeginTransaction();

        using (var insertSources = connection.CreateCommand())
        {
            insertSources.Transaction = tx;
            insertSources.CommandText =
                """
                INSERT OR IGNORE INTO localization_source (entity_type, entity_id, field, source_text, source_hash, updated_utc)
                SELECT
                    entity_type,
                    entity_id,
                    field,
                    '' as source_text,
                    source_hash,
                    updated_utc
                FROM translations;
                """;
            insertSources.ExecuteNonQuery();
        }

        using (var insertTranslations = connection.CreateCommand())
        {
            insertTranslations.Transaction = tx;
            insertTranslations.CommandText =
                """
                INSERT OR IGNORE INTO localization_translation (
                    entity_type, entity_id, field, lang,
                    source_hash, translated_text,
                    status, updated_utc
                )
                SELECT
                    entity_type, entity_id, field, lang,
                    source_hash, text,
                    $ok as status, updated_utc
                FROM translations;
                """;
            insertTranslations.Parameters.AddWithValue("$ok", (int)TranslationStatus.Ok);
            insertTranslations.ExecuteNonQuery();
        }

        tx.Commit();
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

    private static string NormalizeText(string text)
    {
        if (text == null)
        {
            return string.Empty;
        }

        // 1) Trim suave
        var t = text.Trim();

        // 2) Normalizar saltos: CRLF/CR -> LF
        t = t.Replace("\r\n", "\n").Replace("\r", "\n");

        // 3) (Opcional) evitar "cambió por espacios tontos"
        //    No colapsamos todos los espacios porque puede afectar formatos,
        //    pero sí quitamos espacios al final de línea.
        var lines = t.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();
        }

        return string.Join("\n", lines);
    }

    private static string ComputeSha256Hex(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static string RequireNotBlank(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null/empty.", paramName);
        }
        return value;
    }
}

public readonly record struct LocalizationKey(string EntityType, string EntityId, string Field);
