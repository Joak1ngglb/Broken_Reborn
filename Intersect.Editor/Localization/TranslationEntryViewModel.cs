using System;
using System.ComponentModel;
using Intersect.Framework.Core.Localization;

namespace Intersect.Editor.Localization;

public sealed class TranslationEntryViewModel : INotifyPropertyChanged
{
    private string _sourceText;
    private string _sourceHash;
    private TranslationStatus _status;
    private DateTime _lastUpdated;
    private string _fallbackText;
    private string _languageName;
    private string _translationText;

    public TranslationEntryViewModel(
        string sourceText,
        string sourceHash,
        TranslationStatus status,
        DateTime lastUpdated,
        string fallbackText,
        string languageName = "",
        string translationText = ""
    )
    {
        _sourceText = sourceText;
        _sourceHash = sourceHash;
        _status = status;
        _lastUpdated = lastUpdated;
        _fallbackText = fallbackText;
        _languageName = languageName;
        _translationText = translationText;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SourceText
    {
        get => _sourceText;
        set => SetField(ref _sourceText, value);
    }

    public string SourceHash
    {
        get => _sourceHash;
        set => SetField(ref _sourceHash, value);
    }

    public TranslationStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public DateTime LastUpdated
    {
        get => _lastUpdated;
        set => SetField(ref _lastUpdated, value);
    }

    public string FallbackText
    {
        get => _fallbackText;
        set => SetField(ref _fallbackText, value);
    }

    public string LanguageName
    {
        get => _languageName;
        set => SetField(ref _languageName, value);
    }

    public string TranslationText
    {
        get => _translationText;
        set => SetField(ref _translationText, value);
    }

    private void SetField<T>(ref T field, T value)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
