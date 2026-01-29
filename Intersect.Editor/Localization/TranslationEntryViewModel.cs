using System;
using Intersect.Framework.Core.Localization;

namespace Intersect.Editor.Localization;

public sealed class TranslationEntryViewModel
{
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
        SourceText = sourceText;
        SourceHash = sourceHash;
        Status = status;
        LastUpdated = lastUpdated;
        FallbackText = fallbackText;
        LanguageName = languageName;
        TranslationText = translationText;
    }

    public string SourceText { get; set; }

    public string SourceHash { get; set; }

    public TranslationStatus Status { get; set; }

    public DateTime LastUpdated { get; set; }

    public string FallbackText { get; set; }

    public string LanguageName { get; set; }

    public string TranslationText { get; set; }
}
