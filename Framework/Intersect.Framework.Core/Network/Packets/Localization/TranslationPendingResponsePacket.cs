using System.Collections.Generic;
using Intersect.Framework.Core.Localization;
using Intersect.Network.Packets;
using MessagePack;

namespace Intersect.Network.Packets.Localization;

[MessagePackObject]
public sealed partial class TranslationPendingResponsePacket : EditorPacket
{
    public TranslationPendingResponsePacket()
    {
    }

    public TranslationPendingResponsePacket(
        IReadOnlyList<TranslationPendingEntry> entries,
        long totalCount
    )
    {
        Entries = entries;
        TotalCount = totalCount;
    }

    [Key(0)]
    public IReadOnlyList<TranslationPendingEntry> Entries { get; set; } = new List<TranslationPendingEntry>();

    [Key(1)]
    public long TotalCount { get; set; }
}

[MessagePackObject]
public sealed class TranslationPendingEntry
{
    public TranslationPendingEntry()
    {
    }

    public TranslationPendingEntry(
        string entityType,
        string entityId,
        string field,
        string language,
        string sourceText,
        string sourceHash,
        string translatedText,
        TranslationStatus status,
        string updatedUtc
    )
    {
        EntityType = entityType;
        EntityId = entityId;
        Field = field;
        Language = language;
        SourceText = sourceText;
        SourceHash = sourceHash;
        TranslatedText = translatedText;
        Status = status;
        UpdatedUtc = updatedUtc;
    }

    [Key(0)]
    public string EntityType { get; set; }

    [Key(1)]
    public string EntityId { get; set; }

    [Key(2)]
    public string Field { get; set; }

    [Key(3)]
    public string Language { get; set; }

    [Key(4)]
    public string SourceText { get; set; }

    [Key(5)]
    public string SourceHash { get; set; }

    [Key(6)]
    public string TranslatedText { get; set; }

    [Key(7)]
    public TranslationStatus Status { get; set; }

    [Key(8)]
    public string UpdatedUtc { get; set; }
}
