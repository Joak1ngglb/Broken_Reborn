using MessagePack;

namespace Intersect.Network.Packets;

[MessagePackObject]
public class TranslationBatchEntry
{
    public TranslationBatchEntry()
    {
    }

    public TranslationBatchEntry(string id, string text)
    {
        Id = id;
        Text = text;
    }

    [Key(0)]
    public string Id { get; set; } = string.Empty;

    [Key(1)]
    public string Text { get; set; } = string.Empty;
}
