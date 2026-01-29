using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Intersect.Editor.Localization;
using Intersect.Editor.Networking;
using Intersect.Framework.Core.Localization;
using Intersect.Network.Packets.Localization;

namespace Intersect.Editor.Forms.WpfWindows;

public sealed class TranslationQueueViewModel : INotifyPropertyChanged, IDisposable
{
    private const int DefaultPageSize = 200;
    private readonly RelayCommand _nextCommand;
    private readonly RelayCommand _previousCommand;
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
            translation.TranslationText = Clipboard.GetText();
        }
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
