using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject]
public sealed partial class TranslationUpsertPacket : EditorPacket
{
    //Parameterless Constructor for MessagePack
    public TranslationUpsertPacket()
    {
    }

    public TranslationUpsertPacket(
        string entityType,
        string entityId,
        string field,
        string language,
        string text,
        string sourceHash
    )
    {
        EntityType = entityType;
        EntityId = entityId;
        Field = field;
        Language = language;
        Text = text;
        SourceHash = sourceHash;
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
    public string Text { get; set; }

    [Key(5)]
    public string SourceHash { get; set; }
}
