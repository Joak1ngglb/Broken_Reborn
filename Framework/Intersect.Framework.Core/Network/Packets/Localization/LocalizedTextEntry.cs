using Intersect.Framework.Core.Localization;
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
        Status = TranslationStatus.Ok;
    }

    public LocalizedTextEntry(LocalizationRequestEntry request, string text, TranslationStatus status)
    {
        Request = request;
        Text = text;
        Status = status;
    }

    [Key(0)]
    public LocalizationRequestEntry Request { get; set; }

    [Key(1)]
    public string Text { get; set; }

    [Key(2)]
    public TranslationStatus Status { get; set; } = TranslationStatus.Ok;
}
