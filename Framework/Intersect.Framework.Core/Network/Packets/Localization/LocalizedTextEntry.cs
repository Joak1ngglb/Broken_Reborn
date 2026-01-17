using MessagePack;

namespace Intersect.Network.Packets.Localization;

[MessagePackObject]
public sealed partial class LocalizedTextEntry
{
    public LocalizedTextEntry()
    {
    }

    public LocalizedTextEntry(LocalizationRequestEntry request, string text)
    {
        Request = request;
        Text = text;
    }

    [Key(0)]
    public LocalizationRequestEntry Request { get; set; }

    [Key(1)]
    public string Text { get; set; }
}
