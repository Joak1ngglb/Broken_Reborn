using DarkUI.Forms;
using Intersect.Editor.Core;
using Intersect.Editor.Forms;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.Localization;
using Intersect.Framework.Core.Network.Packets;
using Intersect.GameObjects;
using Intersect.Network.Packets.Editor;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Intersect.Editor.Localization;

public static class LocalizationReindexService
{
    private const string ReindexVersionKey = "LocalizationQuestEventReindexVersion";
    private const string PendingVersionKey = "LocalizationQuestEventReindexPending";
    private const string SchemaChecksumKey = "LocalizationQuestEventSchemaChecksum";
    private const string FormatChecksumKey = "LocalizationQuestEventFormatChecksum";

    private const string CurrentReindexVersion = "quest-event-reindex-v1";
    private static readonly string CurrentSchemaChecksum = BuildChecksum(
        string.Join(
            "|",
            QuestFieldKey.Name,
            QuestFieldKey.BeforeDescription,
            QuestFieldKey.StartDescription,
            QuestFieldKey.InProgressDescription,
            QuestFieldKey.EndDescription,
            EventFieldKey.PageDescription(0),
            EventFieldKey.ShowText(0, Guid.Empty, 0),
            EventFieldKey.ChatboxText(0, Guid.Empty, 0),
            EventFieldKey.OptionsText(0, Guid.Empty, 0),
            EventFieldKey.Option(0, Guid.Empty, 0, 0),
            EventFieldKey.InputTitle(0, Guid.Empty, 0),
            EventFieldKey.InputText(0, Guid.Empty, 0),
            EventFieldKey.PlayerLabel(0, Guid.Empty, 0)
        )
    );

    private static readonly string CurrentFormatChecksum = BuildChecksum("translation_upsert:entity_type,entity_id,field,source,language,translated,status");
    private static bool _initialReindexCheckDone;

    public static void EnsureInitialQuestEventReindexIfNeeded(IWin32Window owner)
    {
        if (_initialReindexCheckDone)
        {
            return;
        }

        _initialReindexCheckDone = true;

        var pendingVersion = string.Equals(Preferences.LoadPreference(PendingVersionKey), "true", StringComparison.OrdinalIgnoreCase);
        var storedVersion = Preferences.LoadPreference(ReindexVersionKey);
        var storedSchemaChecksum = Preferences.LoadPreference(SchemaChecksumKey);
        var storedFormatChecksum = Preferences.LoadPreference(FormatChecksumKey);

        var requiresReindex = pendingVersion ||
                              !string.Equals(storedVersion, CurrentReindexVersion, StringComparison.Ordinal) ||
                              !string.Equals(storedSchemaChecksum, CurrentSchemaChecksum, StringComparison.Ordinal) ||
                              !string.Equals(storedFormatChecksum, CurrentFormatChecksum, StringComparison.Ordinal);

        if (!requiresReindex)
        {
            return;
        }

        Preferences.SavePreference(PendingVersionKey, true.ToString());

        var response = DarkMessageBox.ShowWarning(
            "Se detectó una marca pendiente o cambios en el esquema/formato de localización de Quest/Event. ¿Deseas ejecutar una reindexación inicial ahora?",
            "Reindexación inicial de localización",
            DarkDialogButton.YesNo,
            Program.Icon
        );

        if (response != DialogResult.Yes)
        {
            return;
        }

        ReindexQuestAndEventLocalization(owner, "Apertura del editor");
    }

    public static void UpdateIncrementalEventSources(EventDescriptor eventDescriptor)
    {
        var entries = TranslationSourceUpdater.GetEventSources(eventDescriptor);
        QueueEntries(entries, logSummary: true, operationName: "Incremental event save");
    }

    public static void UpdateIncrementalQuestAndEventSources(QuestDescriptor quest)
    {
        var entries = TranslationSourceUpdater.GetQuestAndRelatedEventSources(quest);
        QueueEntries(entries, logSummary: true, operationName: "Incremental quest save");
    }

    public static void ReindexQuestAndEventLocalization(IWin32Window owner, string reason)
    {
        using var progress = new FrmProgress();
        progress.SetTitle("Reindexar localización Quest/Event");
        progress.Show(owner as Form);
        progress.SetProgress($"Iniciando reindexación ({reason})...", 0, false);

        var collectedEntries = new List<TranslationUpsertEntry>();

        var eventDescriptors = EventDescriptor.Lookup.Values.OfType<EventDescriptor>().ToList();
        var questDescriptors = QuestDescriptor.Lookup.Values.OfType<QuestDescriptor>().ToList();
        var totalSteps = Math.Max(1, eventDescriptors.Count + questDescriptors.Count);
        var currentStep = 0;

        foreach (var eventDescriptor in eventDescriptors)
        {
            collectedEntries.AddRange(TranslationSourceUpdater.GetEventSources(eventDescriptor));
            currentStep++;
            progress.SetProgress($"Reindexando eventos ({currentStep}/{totalSteps})...", (int)(currentStep * 100f / totalSteps), false);
        }

        foreach (var questDescriptor in questDescriptors)
        {
            collectedEntries.AddRange(TranslationSourceUpdater.GetQuestAndRelatedEventSources(questDescriptor));
            currentStep++;
            progress.SetProgress($"Reindexando quests ({currentStep}/{totalSteps})...", (int)(currentStep * 100f / totalSteps), false);
        }

        var summary = QueueEntries(collectedEntries, logSummary: true, operationName: $"Bulk reindex ({reason})");

        progress.SetProgress("Finalizando reindexación...", 100, false);
        progress.NotifyClose();

        if (summary.DiscardedEntries > 0)
        {
            DarkMessageBox.ShowWarning(BuildSummaryMessage(summary), "Reindexación completada con descartes", DarkDialogButton.Ok, Program.Icon);
        }
        else
        {
            DarkMessageBox.ShowInformation(BuildSummaryMessage(summary), "Reindexación completada", DarkDialogButton.Ok, Program.Icon);
        }

        if (summary.DiscardedEntries == 0)
        {
            SaveCurrentReindexMarkers();
        }
    }

    private static ReindexSummary QueueEntries(IEnumerable<TranslationUpsertEntry> entries, bool logSummary, string operationName)
    {
        var sentByEntityType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var validEntries = new List<TranslationUpsertEntry>();
        var errors = new List<string>();

        foreach (var entry in entries ?? Enumerable.Empty<TranslationUpsertEntry>())
        {
            if (!IsValidEntry(entry, out var error))
            {
                errors.Add(error);
                continue;
            }

            validEntries.Add(entry);
            sentByEntityType[entry.EntityType] = sentByEntityType.TryGetValue(entry.EntityType, out var total)
                ? total + 1
                : 1;
        }

        TranslationSourceUpdater.QueueBatchSources(validEntries);

        var summary = new ReindexSummary(validEntries.Count + errors.Count, validEntries.Count, errors.Count, sentByEntityType, errors);

        if (logSummary)
        {
            LogSummary(summary, operationName);
        }

        return summary;
    }

    private static bool IsValidEntry(TranslationUpsertEntry entry, out string error)
    {
        if (entry == null)
        {
            error = "Entry nula descartada.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.EntityType))
        {
            error = $"Entry descartada por entity_type vacío (field={entry.Field ?? "<null>"}).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.EntityId))
        {
            error = $"Entry descartada por entity_id vacío (entity_type={entry.EntityType}, field={entry.Field ?? "<null>"}).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.Field))
        {
            error = $"Entry descartada por field vacío (entity_type={entry.EntityType}, entity_id={entry.EntityId}).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static void LogSummary(ReindexSummary summary, string operationName)
    {
        var logger = Intersect.Core.ApplicationContext.Context.Value?.Logger;
        if (logger == null)
        {
            return;
        }

        var entitySummary = summary.SentByEntityType.Count == 0
            ? "none"
            : string.Join(", ", summary.SentByEntityType.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));

        logger.LogInformation(
            "{OperationName}: total_entries={TotalEntries}, sent_entries={SentEntries}, discarded_entries={DiscardedEntries}, sent_by_entity_type={SentByEntityType}",
            operationName,
            summary.TotalEntries,
            summary.SentEntries,
            summary.DiscardedEntries,
            entitySummary
        );

        foreach (var error in summary.Errors.Take(10))
        {
            logger.LogWarning("Localization reindex discarded entry: {Error}", error);
        }
    }

    private static string BuildSummaryMessage(ReindexSummary summary)
    {
        var details = summary.SentByEntityType.Count == 0
            ? "(sin entradas enviadas)"
            : string.Join(Environment.NewLine, summary.SentByEntityType.OrderBy(pair => pair.Key).Select(pair => $"- {pair.Key}: {pair.Value}"));

        var discardedInfo = summary.DiscardedEntries == 0
            ? "Sin entradas descartadas."
            : $"Entradas descartadas: {summary.DiscardedEntries}.{Environment.NewLine}{string.Join(Environment.NewLine, summary.Errors.Take(5).Select(error => $"  • {error}"))}";

        return $"Total detectadas: {summary.TotalEntries}{Environment.NewLine}" +
               $"Total enviadas: {summary.SentEntries}{Environment.NewLine}" +
               $"Resumen por entity_type:{Environment.NewLine}{details}{Environment.NewLine}{discardedInfo}";
    }

    private static string BuildChecksum(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        return Convert.ToHexString(bytes);
    }

    private static void SaveCurrentReindexMarkers()
    {
        Preferences.SavePreference(PendingVersionKey, false.ToString());
        Preferences.SavePreference(ReindexVersionKey, CurrentReindexVersion);
        Preferences.SavePreference(SchemaChecksumKey, CurrentSchemaChecksum);
        Preferences.SavePreference(FormatChecksumKey, CurrentFormatChecksum);
    }

    private sealed record ReindexSummary(
        int TotalEntries,
        int SentEntries,
        int DiscardedEntries,
        IReadOnlyDictionary<string, int> SentByEntityType,
        IReadOnlyList<string> Errors
    );
}
