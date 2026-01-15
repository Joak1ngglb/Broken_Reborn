using MessagePack;
using Intersect.Network.Packets.Localization;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class LocalizedTextRequestPacket : IntersectPacket
{
    public LocalizedTextRequestPacket()
    {
    }

    public LocalizedTextRequestPacket(string language, List<LocalizationRequestEntry> requests)
    {
        Language = language;
        Requests = requests;
    }

    [Key(0)]
    public string Language { get; set; }

    [Key(1)]
    public List<LocalizationRequestEntry> Requests { get; set; }
}
