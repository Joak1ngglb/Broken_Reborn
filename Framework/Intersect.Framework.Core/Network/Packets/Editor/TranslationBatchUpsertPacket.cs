using System.Collections.Generic;
using Intersect.Framework.Core.Localization;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject]
public sealed partial class TranslationBatchUpsertPacket : EditorPacket
{
    public TranslationBatchUpsertPacket()
    {
    }

    public TranslationBatchUpsertPacket(List<TranslationUpsertEntry> entries)
    {
        Entries = entries;
    }

    [Key(0)]
    public List<TranslationUpsertEntry> Entries { get; set; } = new();
}

[MessagePackObject]
public sealed class TranslationUpsertEntry
{
    public TranslationUpsertEntry()
    {
    }

    public TranslationUpsertEntry(
        string entityType,
        string entityId,
        string field,
        string sourceText,
        string language,
        string translatedText,
        TranslationStatus status
    )
    {
        EntityType = entityType;
        EntityId = entityId;
        Field = field;
        SourceText = sourceText;
        Language = language;
        TranslatedText = translatedText;
        Status = status;
    }

    // SourceKey
    [Key(0)]
    public string EntityType { get; set; }

    [Key(1)]
    public string EntityId { get; set; }

    [Key(2)]
    public string Field { get; set; }

    // Source
    [Key(3)]
    public string SourceText { get; set; }

    // Translation
    [Key(4)]
    public string Language { get; set; }

    [Key(5)]
    public string TranslatedText { get; set; }

    // 0=OK, 1=NEEDS_REVIEW, 2=MISSING, 3=MACHINE
    [Key(6)]
    public TranslationStatus Status { get; set; }
}
