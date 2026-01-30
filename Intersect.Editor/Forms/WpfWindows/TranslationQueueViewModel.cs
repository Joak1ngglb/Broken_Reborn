using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Intersect.Editor.Localization;
using Intersect.Editor.Networking;
using Intersect.Framework.Core.Localization;
using Intersect.Network.Packets.Localization;
using Intersect.Network.Packets.Editor;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;

namespace Intersect.Editor.Forms.WpfWindows;

public sealed class TranslationQueueViewModel : INotifyPropertyChanged, IDisposable
{
    private const int DefaultPageSize = 200;
    private readonly RelayCommand _nextCommand;
    private readonly RelayCommand _previousCommand;
    private readonly RelayCommand _exportCsvCommand;
    private readonly RelayCommand _importCsvCommand;
    private readonly RelayCommand _setBrokenStatusCommand;
    private string? _entityIdFilter;
    private bool _suppressRefresh;
    private int _offset;
    private long _totalCount;
    private FilterOption<string?>? _selectedEntityType;
    private FilterOption<TranslationStatus?>? _selectedStatus;
    private FilterOption<string?>? _selectedScope;
    private string _searchText = string.Empty;
    private TranslationQueueEntryViewModel? _selectedEntry;

    public TranslationQueueViewModel()
    {
        EntityTypes = BuildEntityTypes();
        Statuses = BuildStatuses();
        Scopes = BuildScopes();

        _selectedEntityType = EntityTypes.FirstOrDefault();
        _selectedStatus = Statuses.FirstOrDefault();
        _selectedScope = Scopes.FirstOrDefault();

        _previousCommand = new RelayCommand(_ => MovePrevious(), _ => _offset > 0);
        _nextCommand = new RelayCommand(_ => MoveNext(), _ => _offset + DefaultPageSize < _totalCount);
        _exportCsvCommand = new RelayCommand(_ => ExportCsv(), _ => TranslationRepository.Default.LastPendingEntries.Count > 0);
        _importCsvCommand = new RelayCommand(_ => ImportCsv());
        _setBrokenStatusCommand = new RelayCommand(_ => SetBrokenStatus());

        TranslationRepository.Default.PendingTranslationsUpdated += OnPendingTranslationsUpdated;
        RefreshPending();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<FilterOption<string?>> EntityTypes { get; }

    public FilterOption<string?>? SelectedEntityType
    {
        get => _selectedEntityType;
        set => SetFilter(ref _selectedEntityType, value, nameof(SelectedEntityType));
    }

    public IReadOnlyList<FilterOption<TranslationStatus?>> Statuses { get; }

    public FilterOption<TranslationStatus?>? SelectedStatus
    {
        get => _selectedStatus;
        set => SetFilter(ref _selectedStatus, value, nameof(SelectedStatus));
    }

    public IReadOnlyList<FilterOption<string?>> Scopes { get; }

    public FilterOption<string?>? SelectedScope
    {
        get => _selectedScope;
        set => SetFilter(ref _selectedScope, value, nameof(SelectedScope));
    }

    public string SearchText
    {
        get => _searchText;
        set => SetFilter(ref _searchText, value ?? string.Empty, nameof(SearchText));
    }

    public ObservableCollection<TranslationQueueEntryViewModel> FilteredEntries { get; } = new();

    public TranslationQueueEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value, nameof(SelectedEntry));
    }

    public ICommand PreviousCommand => _previousCommand;

    public ICommand NextCommand => _nextCommand;

    public ICommand ExportCsvCommand => _exportCsvCommand;

    public ICommand ImportCsvCommand => _importCsvCommand;

    public ICommand SetBrokenStatusCommand => _setBrokenStatusCommand;

    public void ApplyFilter(string? entityType, string? entityId, string? searchText = null)
    {
        _suppressRefresh = true;
        try
        {
            _entityIdFilter = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim();
            _offset = 0;

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                var match = EntityTypes.FirstOrDefault(option =>
                    string.Equals(option.Value, entityType.Trim(), StringComparison.OrdinalIgnoreCase)
                );
                if (match != null)
                {
                    _selectedEntityType = match;
                    OnPropertyChanged(nameof(SelectedEntityType));
                }
            }

            if (searchText != null)
            {
                _searchText = searchText;
                OnPropertyChanged(nameof(SearchText));
            }
        }
        finally
        {
            _suppressRefresh = false;
        }

        RefreshPending();
    }

    public void Dispose()
    {
        TranslationRepository.Default.PendingTranslationsUpdated -= OnPendingTranslationsUpdated;
    }

    private void MoveNext()
    {
        if (_offset + DefaultPageSize >= _totalCount)
        {
            return;
        }

        _offset += DefaultPageSize;
        RefreshPending();
    }

    private void MovePrevious()
    {
        if (_offset <= 0)
        {
            return;
        }

        _offset = Math.Max(0, _offset - DefaultPageSize);
        RefreshPending();
    }

    private void AdvanceEntry()
    {
        if (SelectedEntry == null)
        {
            return;
        }

        var currentIndex = FilteredEntries.IndexOf(SelectedEntry);
        if (currentIndex < 0)
        {
            return;
        }

        if (currentIndex + 1 < FilteredEntries.Count)
        {
            SelectedEntry = FilteredEntries[currentIndex + 1];
        }
        else
        {
            MoveNext();
        }
    }

    private void RefreshPending()
    {
        var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        var entityType = SelectedEntityType?.Value;
        var status = SelectedStatus?.Value;
        var language = SelectedScope?.Value;

        TranslationRepository.Default.RequestPending(
            entityType,
            _entityIdFilter,
            status,
            search,
            language,
            DefaultPageSize,
            _offset
        );
    }

    private void OnPendingTranslationsUpdated(IReadOnlyList<TranslationPendingEntry> entries, long totalCount)
    {
        void UpdateEntries()
        {
            _totalCount = totalCount;
            _nextCommand.RaiseCanExecuteChanged();
            _previousCommand.RaiseCanExecuteChanged();
            _exportCsvCommand.RaiseCanExecuteChanged();

            FilteredEntries.Clear();
            foreach (var entry in entries)
            {
                FilteredEntries.Add(new TranslationQueueEntryViewModel(entry, AdvanceEntry));
            }

            SelectedEntry = FilteredEntries.FirstOrDefault();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(UpdateEntries);
        }
        else
        {
            UpdateEntries();
        }
    }

    private void SetFilter<T>(ref T field, T value, string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);

        if (!_suppressRefresh)
        {
            _offset = 0;
            RefreshPending();
        }
    }

    private void SetProperty<T>(ref T field, T value, string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetBrokenStatus()
    {
        var brokenOption = Statuses.FirstOrDefault(option => option.Value == TranslationStatus.Broken);
        if (brokenOption != null)
        {
            SelectedStatus = brokenOption;
        }
    }

    private void ExportCsv()
    {
        var entries = TranslationRepository.Default.LastPendingEntries;
        if (entries.Count == 0)
        {
            MessageBox.Show("No pending entries to export.", "Translation Workbench", MessageBoxButton.OK);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = BuildExportFileName(),
            AddExtension = true,
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
        writer.WriteLine(
            string.Join(
                ",",
                "EntityType",
                "EntityId",
                "EntityName",
                "Field",
                "Language",
                "Status",
                "SourceText",
                "TranslatedText",
                "UpdatedUtc"
            )
        );

        foreach (var entry in entries)
        {
            writer.WriteLine(string.Join(
                ",",
                EscapeCsv(entry.EntityType),
                EscapeCsv(entry.EntityId),
                EscapeCsv(entry.EntityName ?? string.Empty),
                EscapeCsv(entry.Field),
                EscapeCsv(entry.Language),
                EscapeCsv(entry.Status.ToString()),
                EscapeCsv(entry.SourceText),
                EscapeCsv(entry.TranslatedText),
                EscapeCsv(entry.UpdatedUtc)
            ));
        }

        MessageBox.Show(
            $"Exported {entries.Count} pending entries.",
            "Translation Workbench",
            MessageBoxButton.OK
        );
    }

    private void ImportCsv()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var (entries, message) = ParseCsvTranslations(dialog.FileName);
            if (!string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(message, "Translation Workbench", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (entries.Count == 0)
            {
                MessageBox.Show("No translations to import.", "Translation Workbench", MessageBoxButton.OK);
                return;
            }

            SendTranslationBatches(entries);

            MessageBox.Show(
                $"Imported {entries.Count} translations.",
                "Translation Workbench",
                MessageBoxButton.OK
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to import CSV: {ex.Message}",
                "Translation Workbench",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private static void SendTranslationBatches(IReadOnlyList<TranslationUpsertEntry> entries, int batchSize = 200)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var batch = new List<TranslationUpsertEntry>(batchSize);
        foreach (var entry in entries)
        {
            batch.Add(entry);
            if (batch.Count < batchSize)
            {
                continue;
            }

            PacketSender.SendTranslationBatchUpsert(batch);
            batch = new List<TranslationUpsertEntry>(batchSize);
        }

        if (batch.Count > 0)
        {
            PacketSender.SendTranslationBatchUpsert(batch);
        }
    }

    private static (List<TranslationUpsertEntry> Entries, string? Message) ParseCsvTranslations(string fileName)
    {
        using var parser = new TextFieldParser(fileName)
        {
            HasFieldsEnclosedInQuotes = true,
        };

        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            return (new List<TranslationUpsertEntry>(), "The CSV file is empty.");
        }

        var headers = parser.ReadFields() ?? Array.Empty<string>();
        if (headers.Length == 0)
        {
            return (new List<TranslationUpsertEntry>(), "The CSV file does not contain a header row.");
        }

        var headerLookup = headers
            .Select((header, index) => (header: header.Trim(), index))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.header))
            .GroupBy(entry => entry.header, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.OrdinalIgnoreCase);

        var keyIndex = GetColumnIndex(headerLookup, "TranslationEntryKey", "EntryKey", "Key");
        var entityTypeIndex = GetColumnIndex(headerLookup, "EntityType");
        var entityIdIndex = GetColumnIndex(headerLookup, "EntityId", "EntityID", "Id");
        var fieldIndex = GetColumnIndex(headerLookup, "Field");
        var subPathIndex = GetColumnIndex(headerLookup, "SubPath", "Subpath");
        var languageIndex = GetColumnIndex(headerLookup, "Language", "Lang", "Locale");
        var translatedTextIndex = GetColumnIndex(headerLookup, "TranslatedText", "Translation", "Translated");
        var sourceTextIndex = GetColumnIndex(headerLookup, "SourceText", "Source");
        var statusIndex = GetColumnIndex(headerLookup, "Status");

        if (translatedTextIndex < 0)
        {
            return (new List<TranslationUpsertEntry>(), "Missing the required TranslatedText column.");
        }

        if (languageIndex < 0)
        {
            return (new List<TranslationUpsertEntry>(), "Missing the required Language column.");
        }

        if (keyIndex < 0 && (entityTypeIndex < 0 || entityIdIndex < 0 || fieldIndex < 0))
        {
            return (new List<TranslationUpsertEntry>(), "Missing TranslationEntryKey or EntityType/EntityId/Field columns.");
        }

        var entries = new List<TranslationUpsertEntry>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields() ?? Array.Empty<string>();
            if (fields.Length == 0)
            {
                continue;
            }

            var translationText = GetField(fields, translatedTextIndex);
            var language = GetField(fields, languageIndex);

            if (string.IsNullOrWhiteSpace(language))
            {
                continue;
            }

            if (!TryGetEntryKey(
                    fields,
                    keyIndex,
                    entityTypeIndex,
                    entityIdIndex,
                    fieldIndex,
                    subPathIndex,
                    out var entryKey
                ) ||
                entryKey == null)
            {
                continue;
            }

            var sourceText = GetField(fields, sourceTextIndex);
            var status = GetStatus(fields, statusIndex);
            var field = CombineField(entryKey.Field, entryKey.SubPath);

            entries.Add(new TranslationUpsertEntry(
                entryKey.EntityType,
                entryKey.EntityId,
                field,
                sourceText,
                language,
                translationText,
                status
            ));
        }

        return (entries, null);
    }

    private static bool TryGetEntryKey(
        string[] fields,
        int keyIndex,
        int entityTypeIndex,
        int entityIdIndex,
        int fieldIndex,
        int subPathIndex,
        out TranslationEntryKey? entryKey
    )
    {
        entryKey = null;
        if (keyIndex >= 0)
        {
            var serialized = GetField(fields, keyIndex);
            if (!string.IsNullOrWhiteSpace(serialized) &&
                TranslationEntryKey.TryParseSerialized(serialized, out entryKey))
            {
                return true;
            }
        }

        var entityType = GetField(fields, entityTypeIndex);
        var entityId = GetField(fields, entityIdIndex);
        var field = GetField(fields, fieldIndex);
        var subPath = GetField(fields, subPathIndex);

        if (string.IsNullOrWhiteSpace(entityType) ||
            string.IsNullOrWhiteSpace(entityId) ||
            string.IsNullOrWhiteSpace(field))
        {
            return false;
        }

        entryKey = new TranslationEntryKey(entityType, entityId, field, subPath);
        return true;
    }

    private static string CombineField(string field, string subPath)
    {
        if (string.IsNullOrWhiteSpace(subPath))
        {
            return field;
        }

        if (string.IsNullOrWhiteSpace(field))
        {
            return subPath;
        }

        if (field.StartsWith(subPath, StringComparison.OrdinalIgnoreCase))
        {
            return field;
        }

        return $"{subPath}:{field}";
    }

    private static TranslationStatus GetStatus(string[] fields, int statusIndex)
    {
        if (statusIndex < 0)
        {
            return TranslationStatus.Ok;
        }

        var statusValue = GetField(fields, statusIndex);
        if (Enum.TryParse(statusValue, true, out TranslationStatus status))
        {
            return status;
        }

        return TranslationStatus.Ok;
    }

    private static string GetField(string[] fields, int index)
    {
        if (index < 0 || index >= fields.Length)
        {
            return string.Empty;
        }

        return fields[index] ?? string.Empty;
    }

    private static int GetColumnIndex(
        IReadOnlyDictionary<string, int> headerLookup,
        params string[] candidates
    )
    {
        foreach (var candidate in candidates)
        {
            if (headerLookup.TryGetValue(candidate, out var index))
            {
                return index;
            }
        }

        return -1;
    }

    private string BuildExportFileName()
    {
        var statusLabel = SelectedStatus?.Value?.ToString() ?? "All";
        var scopeLabel = SelectedScope?.Value ?? "All";
        var entityType = SelectedEntityType?.Value ?? "All";
        var suffix = $"{entityType}_{statusLabel}_{scopeLabel}".Replace(" ", string.Empty);
        return $"translation-pending-{suffix}.csv";
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private static IReadOnlyList<FilterOption<string?>> BuildEntityTypes()
    {
        var types = typeof(LocalizationEntityTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => field.GetRawConstantValue() as string)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .OrderBy(value => value)
            .Select(value => new FilterOption<string?>(value ?? string.Empty, value))
            .ToList();

        types.Insert(0, new FilterOption<string?>("All", null));
        return types;
    }

    private static IReadOnlyList<FilterOption<TranslationStatus?>> BuildStatuses()
    {
        var statuses = Enum.GetValues<TranslationStatus>()
            .Select(status => new FilterOption<TranslationStatus?>(status.ToString(), status))
            .ToList();

        statuses.Insert(0, new FilterOption<TranslationStatus?>("All", null));
        return statuses;
    }

    private static IReadOnlyList<FilterOption<string?>> BuildScopes()
    {
        return new List<FilterOption<string?>>
        {
            new("All", null),
        };
    }
}

public sealed class TranslationQueueEntryViewModel
{
    private readonly TranslationPendingEntry _entry;
    private readonly Action _advanceAction;
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _saveAndNextCommand;
    private readonly RelayCommand _copySourceCommand;
    private readonly RelayCommand _quickPasteCommand;
    private static readonly Regex QuickPasteRegex =
        new(@"^\s*(?<lang>[\w-]+)\)\s*=\s*(?<text>.*)\s*$", RegexOptions.Compiled);

    public TranslationQueueEntryViewModel(TranslationPendingEntry entry, Action advanceAction)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        _advanceAction = advanceAction ?? throw new ArgumentNullException(nameof(advanceAction));

        EntityType = entry.EntityType;
        EntityId = entry.EntityId;
        EntityNameDisplay = string.IsNullOrWhiteSpace(entry.EntityName) ? string.Empty : $" - {entry.EntityName}";
        var (field, subPath) = SplitField(entry.Field);
        Field = field;
        SubPath = subPath;
        Status = entry.Status.ToString();

        var updatedUtc = DateTime.TryParse(
            entry.UpdatedUtc,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed
        )
            ? parsed
            : DateTime.UtcNow;

        TranslationEntries = new ObservableCollection<TranslationEntryViewModel>
        {
            new(
                entry.SourceText,
                entry.SourceHash,
                entry.Status,
                updatedUtc,
                string.Empty,
                entry.Language,
                entry.TranslatedText
            ),
        };

        _saveCommand = new RelayCommand(_ => Save());
        _saveAndNextCommand = new RelayCommand(_ =>
        {
            Save();
            _advanceAction();
        });
        _copySourceCommand = new RelayCommand(_ => CopySource());
        _quickPasteCommand = new RelayCommand(_ => QuickPaste());
    }

    public string EntityType { get; }

    public string EntityId { get; }

    public string EntityNameDisplay { get; }

    public string Field { get; }

    public string SubPath { get; }

    public string Status { get; }

    public ObservableCollection<TranslationEntryViewModel> TranslationEntries { get; }

    public ICommand SaveCommand => _saveCommand;

    public ICommand SaveAndNextCommand => _saveAndNextCommand;

    public ICommand CopySourceCommand => _copySourceCommand;

    public ICommand QuickPasteCommand => _quickPasteCommand;

    private TranslationEntryViewModel? GetActiveTranslation() => TranslationEntries.FirstOrDefault();

    private static (string Field, string SubPath) SplitField(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return (string.Empty, string.Empty);
        }

        var tokens = field.Split(':');
        if (tokens.Length >= 3 &&
            string.Equals(tokens[0], "Page", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(tokens[1], out _))
        {
            if (tokens.Length >= 6 &&
                string.Equals(tokens[2], "List", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(tokens[4], "Command", StringComparison.OrdinalIgnoreCase))
            {
                var subPath = string.Join(":", tokens.Take(6));
                var remainder = string.Join(":", tokens.Skip(6));
                return (string.IsNullOrWhiteSpace(remainder) ? field : remainder, subPath);
            }

            var pageSubPath = string.Join(":", tokens.Take(2));
            var pageRemainder = string.Join(":", tokens.Skip(2));
            return (string.IsNullOrWhiteSpace(pageRemainder) ? field : pageRemainder, pageSubPath);
        }

        return (field, string.Empty);
    }

    private void Save()
    {
        var translation = GetActiveTranslation();
        if (translation == null)
        {
            return;
        }

        PacketSender.SendTranslationUpsert(
            _entry.EntityType,
            _entry.EntityId,
            _entry.Field,
            _entry.Language,
            translation.TranslationText ?? string.Empty,
            _entry.SourceHash
        );
    }

    private void CopySource()
    {
        if (!string.IsNullOrWhiteSpace(_entry.SourceText))
        {
            Clipboard.SetText(_entry.SourceText);
        }
    }

    private void QuickPaste()
    {
        if (!Clipboard.ContainsText())
        {
            return;
        }

        var translation = GetActiveTranslation();
        if (translation != null)
        {
            var clipboardText = Clipboard.GetText();
            if (TryParseQuickPaste(clipboardText, out var translations))
            {
                if (translations.TryGetValue(translation.LanguageName, out var matchedText))
                {
                    translation.TranslationText = matchedText;
                }
                else if (translations.Count == 1)
                {
                    translation.TranslationText = translations.Values.First();
                }
            }
            else
            {
                translation.TranslationText = clipboardText;
            }
        }
    }

    private static bool TryParseQuickPaste(string text, out Dictionary<string, string> translations)
    {
        translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            var match = QuickPasteRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var language = match.Groups["lang"].Value.Trim();
            var translation = match.Groups["text"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(language))
            {
                translations[language] = translation;
            }
        }

        return translations.Count > 0;
    }
}

public sealed class FilterOption<T>
{
    public FilterOption(string label, T value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public T Value { get; }

    public override string ToString() => Label;
}

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
