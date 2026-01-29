using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Events.Commands;
using Intersect.Framework.Core.Localization;
using Intersect.Localization;
using Intersect.Network.Packets.Localization;
using Intersect.Server.Core;
using Microsoft.Data.Sqlite;

namespace Intersect.Server.Localization;

public sealed class LocalizationRepository
{
    private const string DefaultLanguage = "en";
    private const int BusyRetryCount = 3;
    private const int BusyRetryDelayMs = 150;
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
        Default.MigrateTranslationStatusConstraintIfNeeded();
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

            CREATE INDEX IF NOT EXISTS idx_localization_translation_entity_type
                ON localization_translation (entity_type);
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
        return ExecuteWithRetry(() =>
        {
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
        });
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

        ExecuteWithRetry(() =>
        {
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
        });
    }

    public void EnsureMissingTranslation(
        string entityType,
        string entityId,
        string field,
        string language,
        string sourceHash
    )
    {
        entityType = RequireNotBlank(entityType, nameof(entityType));
        entityId = RequireNotBlank(entityId, nameof(entityId));
        field = RequireNotBlank(field, nameof(field));

        ExecuteWithRetry(() =>
        {
            var lang = NormalizeLanguage(language);
            var now = DateTime.UtcNow.ToString("O");

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT OR IGNORE INTO localization_translation (
                    entity_type, entity_id, field, lang,
                    source_hash, translated_text,
                    status, updated_utc
                )
                VALUES (
                    $entityType, $entityId, $field, $lang,
                    $sourceHash, '',
                    $status, $updatedUtc
                );
                """;

            command.Parameters.AddWithValue("$entityType", entityType);
            command.Parameters.AddWithValue("$entityId", entityId);
            command.Parameters.AddWithValue("$field", field);
            command.Parameters.AddWithValue("$lang", lang);
            command.Parameters.AddWithValue("$sourceHash", RequireNotBlank(sourceHash, nameof(sourceHash)));
            command.Parameters.AddWithValue("$status", (int)TranslationStatus.Missing);
            command.Parameters.AddWithValue("$updatedUtc", now);

            command.ExecuteNonQuery();
        });
    }

    public string? GetCurrentSourceHash(string entityType, string entityId, string field)
    {
        entityType = RequireNotBlank(entityType, nameof(entityType));
        entityId = RequireNotBlank(entityId, nameof(entityId));
        field = RequireNotBlank(field, nameof(field));

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT source_hash
            FROM localization_source
            WHERE entity_type = $entityType
              AND entity_id = $entityId
              AND field = $field
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$entityType", entityType);
        command.Parameters.AddWithValue("$entityId", entityId);
        command.Parameters.AddWithValue("$field", field);

        var scalar = command.ExecuteScalar();
        return scalar == DBNull.Value ? null : scalar as string;
    }

    public string? GetCurrentSourceText(string entityType, string entityId, string field)
    {
        entityType = RequireNotBlank(entityType, nameof(entityType));
        entityId = RequireNotBlank(entityId, nameof(entityId));
        field = RequireNotBlank(field, nameof(field));

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT source_text
            FROM localization_source
            WHERE entity_type = $entityType
              AND entity_id = $entityId
              AND field = $field
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$entityType", entityType);
        command.Parameters.AddWithValue("$entityId", entityId);
        command.Parameters.AddWithValue("$field", field);

        var scalar = command.ExecuteScalar();
        return scalar == DBNull.Value ? null : scalar as string;
    }

    public static IReadOnlyList<int> GetMissingArgumentIndices(string sourceText, string translatedText)
    {
        var sourceArguments = LocalizedString.GetArgumentIndices(sourceText ?? string.Empty);
        if (sourceArguments.Count == 0)
        {
            return Array.Empty<int>();
        }

        var translatedArguments = LocalizedString.GetArgumentIndices(translatedText ?? string.Empty);
        if (translatedArguments.Count == 0)
        {
            return sourceArguments.OrderBy(argument => argument).ToArray();
        }

        return sourceArguments
            .Where(argument => !translatedArguments.Contains(argument))
            .OrderBy(argument => argument)
            .ToArray();
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

        var normalizedLanguage = NormalizeLanguage(language);

        string? GetTranslationForHash(string lang, string sourceHash)
        {
            using var tr = connection.CreateCommand();
            tr.CommandText =
                """
                SELECT translated_text
                FROM localization_translation
                WHERE entity_type = $entityType
                  AND entity_id = $entityId
                  AND field = $field
                  AND source_hash = $sourceHash
                  AND status NOT IN ($missing, $broken)
                  AND lang = $language
                LIMIT 1;
                """;

            tr.Parameters.AddWithValue("$entityType", entityType);
            tr.Parameters.AddWithValue("$entityId", entityId);
            tr.Parameters.AddWithValue("$field", field);
            tr.Parameters.AddWithValue("$sourceHash", sourceHash);
            tr.Parameters.AddWithValue("$missing", (int)TranslationStatus.Missing);
            tr.Parameters.AddWithValue("$broken", (int)TranslationStatus.Broken);
            tr.Parameters.AddWithValue("$language", lang);

            var result = tr.ExecuteScalar();
            return result == DBNull.Value ? null : result as string;
        }

        string? GetLatestTranslation(string lang)
        {
            using var tr = connection.CreateCommand();
            tr.CommandText =
                """
                SELECT translated_text
                FROM localization_translation
                WHERE entity_type = $entityType
                  AND entity_id = $entityId
                  AND field = $field
                  AND status NOT IN ($missing, $broken)
                  AND lang = $language
                ORDER BY updated_utc DESC
                LIMIT 1;
                """;

            tr.Parameters.AddWithValue("$entityType", entityType);
            tr.Parameters.AddWithValue("$entityId", entityId);
            tr.Parameters.AddWithValue("$field", field);
            tr.Parameters.AddWithValue("$missing", (int)TranslationStatus.Missing);
            tr.Parameters.AddWithValue("$broken", (int)TranslationStatus.Broken);
            tr.Parameters.AddWithValue("$language", lang);

            var result = tr.ExecuteScalar();
            return result == DBNull.Value ? null : result as string;
        }

        var translation = GetTranslationForHash(normalizedLanguage, currentHash);
        if (!string.IsNullOrWhiteSpace(translation))
        {
            return translation;
        }

        translation = GetLatestTranslation(normalizedLanguage);
        if (!string.IsNullOrWhiteSpace(translation))
        {
            return translation;
        }

        if (!string.Equals(normalizedLanguage, DefaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            translation = GetTranslationForHash(DefaultLanguage, currentHash);
            if (!string.IsNullOrWhiteSpace(translation))
            {
                return translation;
            }

            translation = GetLatestTranslation(DefaultLanguage);
            if (!string.IsNullOrWhiteSpace(translation))
            {
                return translation;
            }
        }

        return GetFallbackSourceText(entityType, entityId, field);
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

    private static string? GetFallbackSourceText(string entityType, string entityId, string field)
    {
        if (!string.Equals(entityType, LocalizationEntityTypes.Event, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TryGetEventTextFallback(entityId, field, out var fallback)
            ? fallback
            : null;
    }

    private static bool TryGetEventTextFallback(string entityId, string field, out string? fallback)
    {
        fallback = null;
        if (!Guid.TryParse(entityId, out var eventId))
        {
            return false;
        }

        var eventDescriptor = EventDescriptor.Get(eventId);
        if (eventDescriptor?.Pages == null)
        {
            return false;
        }

        if (!TryParseEventField(field, out var pageIndex, out var listId, out var commandIndex, out var fieldKey))
        {
            return false;
        }

        if (pageIndex < 0 || pageIndex >= eventDescriptor.Pages.Count)
        {
            return false;
        }

        var page = eventDescriptor.Pages[pageIndex];
        if (string.Equals(fieldKey, "Description", StringComparison.OrdinalIgnoreCase))
        {
            fallback = page.Description;
            return true;
        }

        if (listId == Guid.Empty || commandIndex < 0)
        {
            return false;
        }

        if (!page.CommandLists.TryGetValue(listId, out var commands) ||
            commandIndex >= commands.Count)
        {
            return false;
        }

        var command = commands[commandIndex];
        switch (command)
        {
            case ShowTextCommand showText when fieldKey.Equals("ShowText", StringComparison.OrdinalIgnoreCase):
                fallback = showText.Text;
                return true;
            case AddChatboxTextCommand chatboxText when fieldKey.Equals("ChatboxText", StringComparison.OrdinalIgnoreCase):
                fallback = chatboxText.Text;
                return true;
            case ShowOptionsCommand showOptions:
                if (fieldKey.Equals("OptionsText", StringComparison.OrdinalIgnoreCase))
                {
                    fallback = showOptions.Text;
                    return true;
                }

                if (TryParseOptionIndex(fieldKey, out var optionIndex) &&
                    showOptions.Options != null &&
                    optionIndex >= 0 &&
                    optionIndex < showOptions.Options.Length)
                {
                    fallback = showOptions.Options[optionIndex];
                    return true;
                }
                return false;
            case InputVariableCommand inputVariable:
                if (fieldKey.Equals("InputTitle", StringComparison.OrdinalIgnoreCase))
                {
                    fallback = inputVariable.Title;
                    return true;
                }

                if (fieldKey.Equals("InputText", StringComparison.OrdinalIgnoreCase))
                {
                    fallback = inputVariable.Text;
                    return true;
                }
                return false;
            case ChangePlayerLabelCommand changeLabel when fieldKey.Equals("PlayerLabel", StringComparison.OrdinalIgnoreCase):
                fallback = changeLabel.Value;
                return true;
        }

        return false;
    }

    private static bool TryParseEventField(
        string field,
        out int pageIndex,
        out Guid listId,
        out int commandIndex,
        out string fieldKey)
    {
        pageIndex = -1;
        listId = Guid.Empty;
        commandIndex = -1;
        fieldKey = string.Empty;

        if (string.IsNullOrWhiteSpace(field))
        {
            return false;
        }

        var tokens = field.Split(':');
        if (tokens.Length < 3 || !tokens[0].Equals("Page", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(tokens[1], out pageIndex))
        {
            return false;
        }

        if (tokens.Length == 3)
        {
            fieldKey = tokens[2];
            return true;
        }

        if (tokens.Length < 6 ||
            !tokens[2].Equals("List", StringComparison.OrdinalIgnoreCase) ||
            !tokens[4].Equals("Command", StringComparison.OrdinalIgnoreCase))
        {
            fieldKey = string.Join(":", tokens.Skip(2));
            return true;
        }

        if (!Guid.TryParse(tokens[3], out listId))
        {
            return false;
        }

        if (!int.TryParse(tokens[5], out commandIndex))
        {
            return false;
        }

        fieldKey = string.Join(":", tokens.Skip(6));
        return true;
    }

    private static bool TryParseOptionIndex(string fieldKey, out int optionIndex)
    {
        optionIndex = -1;
        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return false;
        }

        var tokens = fieldKey.Split(':');
        if (tokens.Length != 2 || !tokens[0].Equals("Option", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(tokens[1], out optionIndex);
    }

    public IReadOnlyList<LocalizationTranslationStatusCount> GetMissingAndNeedsReviewCounts()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT lang, entity_type, field, COUNT(*)
            FROM localization_translation
            WHERE status IN ($missing, $needsReview, $broken)
            GROUP BY lang, entity_type, field;
            """;
        command.Parameters.AddWithValue("$missing", (int)TranslationStatus.Missing);
        command.Parameters.AddWithValue("$needsReview", (int)TranslationStatus.NeedsReview);
        command.Parameters.AddWithValue("$broken", (int)TranslationStatus.Broken);

        var results = new List<LocalizationTranslationStatusCount>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var lang = reader.GetString(0);
            var entityType = reader.GetString(1);
            var field = reader.GetString(2);
            var count = reader.GetInt64(3);
            results.Add(new LocalizationTranslationStatusCount(lang, entityType, field, count));
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

    private void MigrateTranslationStatusConstraintIfNeeded()
    {
        using var connection = OpenConnection();

        string? createSql;
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                SELECT sql
                FROM sqlite_master
                WHERE type='table' AND name='localization_translation'
                LIMIT 1;
                """;
            createSql = cmd.ExecuteScalar() as string;
        }

        if (string.IsNullOrWhiteSpace(createSql))
        {
            return;
        }

        var normalizedSql = new string(createSql.Where(c => !char.IsWhiteSpace(c)).ToArray())
            .ToLowerInvariant();
        var hasStatusCheck = normalizedSql.Contains("check(statusin(0,1))", StringComparison.Ordinal);

        if (!hasStatusCheck)
        {
            return;
        }

        // Older servers created localization_translation with CHECK(status IN (0,1)).
        // We now support additional statuses, and SQLite cannot drop CHECK constraints
        // in-place, so we rebuild the table only when we detect the legacy constraint.
        using var tx = connection.BeginTransaction();

        using (var rebuild = connection.CreateCommand())
        {
            rebuild.Transaction = tx;
            rebuild.CommandText =
                """
                CREATE TABLE localization_translation_new (
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

                INSERT INTO localization_translation_new (
                    entity_type, entity_id, field, lang,
                    source_hash, translated_text,
                    status, updated_utc
                )
                SELECT
                    entity_type, entity_id, field, lang,
                    source_hash, translated_text,
                    status, updated_utc
                FROM localization_translation;

                DROP TABLE localization_translation;

                ALTER TABLE localization_translation_new RENAME TO localization_translation;

                CREATE INDEX IF NOT EXISTS idx_localization_translation_lookup
                    ON localization_translation (entity_type, entity_id, field, lang);

                CREATE INDEX IF NOT EXISTS idx_localization_translation_lang
                    ON localization_translation (lang);

                CREATE INDEX IF NOT EXISTS idx_localization_translation_status
                    ON localization_translation (status);

                CREATE INDEX IF NOT EXISTS idx_localization_translation_entity_type
                    ON localization_translation (entity_type);
                """;
            rebuild.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private SqliteConnection OpenConnection()
    {
        EnsureDatabaseExists();
        var connection = new SqliteConnection($"Data Source={_databasePath};Default Timeout=5;");
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
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

    private static T ExecuteWithRetry<T>(Func<T> action)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (SqliteException ex) when (IsBusyError(ex) && attempt < BusyRetryCount)
            {
                Thread.Sleep(BusyRetryDelayMs * (attempt + 1));
            }
        }
    }

    private static void ExecuteWithRetry(Action action)
    {
        ExecuteWithRetry<object?>(() =>
        {
            action();
            return null;
        });
    }

    private static bool IsBusyError(SqliteException ex)
    {
        return ex.SqliteErrorCode == 5;
    }

    public (IReadOnlyList<TranslationPendingEntry> Entries, long TotalCount) QueryPending(
        string? entityType,
        string? entityId,
        TranslationStatus? status,
        string? search,
        string? language,
        int limit,
        int offset
    )
    {
        var normalizedEntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim();
        var normalizedEntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim();
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? null : NormalizeLanguage(language);
        var normalizedLimit = limit <= 0 ? 200 : limit;
        var normalizedOffset = offset < 0 ? 0 : offset;

        return ExecuteWithRetry(() =>
        {
            using var connection = OpenConnection();
            var whereClause = new StringBuilder("WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(normalizedEntityType))
            {
                whereClause.Append(" AND lt.entity_type = $entityType");
            }

            if (!string.IsNullOrWhiteSpace(normalizedEntityId))
            {
                whereClause.Append(" AND lt.entity_id = $entityId");
            }

            if (status.HasValue)
            {
                whereClause.Append(" AND lt.status = $status");
            }

            if (!string.IsNullOrWhiteSpace(normalizedLanguage))
            {
                whereClause.Append(" AND lt.lang = $lang");
            }

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                whereClause.Append(
                    " AND (ls.source_text LIKE $search OR lt.translated_text LIKE $search OR lt.entity_id LIKE $search OR lt.field LIKE $search)"
                );
            }

            var entries = new List<TranslationPendingEntry>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    $"""
                    SELECT
                        lt.entity_type,
                        lt.entity_id,
                        lt.field,
                        lt.lang,
                        ls.source_text,
                        lt.source_hash,
                        lt.translated_text,
                        lt.status,
                        lt.updated_utc
                    FROM localization_translation lt
                    JOIN localization_source ls
                        ON ls.entity_type = lt.entity_type
                       AND ls.entity_id = lt.entity_id
                       AND ls.field = lt.field
                    {whereClause}
                    ORDER BY lt.updated_utc DESC
                    LIMIT $limit OFFSET $offset;
                    """;

                if (!string.IsNullOrWhiteSpace(normalizedEntityType))
                {
                    command.Parameters.AddWithValue("$entityType", normalizedEntityType);
                }

                if (!string.IsNullOrWhiteSpace(normalizedEntityId))
                {
                    command.Parameters.AddWithValue("$entityId", normalizedEntityId);
                }

                if (status.HasValue)
                {
                    command.Parameters.AddWithValue("$status", (int)status.Value);
                }

                if (!string.IsNullOrWhiteSpace(normalizedLanguage))
                {
                    command.Parameters.AddWithValue("$lang", normalizedLanguage);
                }

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    command.Parameters.AddWithValue("$search", $"%{normalizedSearch}%");
                }

                command.Parameters.AddWithValue("$limit", normalizedLimit);
                command.Parameters.AddWithValue("$offset", normalizedOffset);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var entityType = reader.GetString(0);
                    var entityId = reader.GetString(1);
                    var entityName = ResolveEntityName(entityType, entityId);
                    entries.Add(new TranslationPendingEntry(
                        entityType,
                        entityId,
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetString(5),
                        reader.GetString(6),
                        (TranslationStatus)reader.GetInt32(7),
                        reader.GetString(8),
                        entityName
                    ));
                }
            }

            long totalCount;
            using (var countCommand = connection.CreateCommand())
            {
                countCommand.CommandText =
                    $"""
                    SELECT COUNT(*)
                    FROM localization_translation lt
                    JOIN localization_source ls
                        ON ls.entity_type = lt.entity_type
                       AND ls.entity_id = lt.entity_id
                       AND ls.field = lt.field
                    {whereClause};
                    """;

                if (!string.IsNullOrWhiteSpace(normalizedEntityType))
                {
                    countCommand.Parameters.AddWithValue("$entityType", normalizedEntityType);
                }

                if (!string.IsNullOrWhiteSpace(normalizedEntityId))
                {
                    countCommand.Parameters.AddWithValue("$entityId", normalizedEntityId);
                }

                if (status.HasValue)
                {
                    countCommand.Parameters.AddWithValue("$status", (int)status.Value);
                }

                if (!string.IsNullOrWhiteSpace(normalizedLanguage))
                {
                    countCommand.Parameters.AddWithValue("$lang", normalizedLanguage);
                }

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    countCommand.Parameters.AddWithValue("$search", $"%{normalizedSearch}%");
                }

                totalCount = Convert.ToInt64(countCommand.ExecuteScalar());
            }

            return (entries, totalCount);
        });
    }

    private static string? ResolveEntityName(string? entityType, string? entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
        {
            return null;
        }

        if (!Guid.TryParse(entityId, out var parsedId))
        {
            return null;
        }

        if (entityType.Equals(GameObjectType.Crafts.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return CraftingRecipeDescriptor.GetName(parsedId);
        }

        if (entityType.Equals(GameObjectType.CraftTables.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return CraftingTableDescriptor.GetName(parsedId);
        }

        return null;
    }
}

public readonly record struct LocalizationKey(string EntityType, string EntityId, string Field);

public readonly record struct LocalizationTranslationStatusCount(
    string Language,
    string EntityType,
    string Field,
    long Count
);
