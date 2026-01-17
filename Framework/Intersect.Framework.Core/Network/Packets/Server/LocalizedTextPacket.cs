using MessagePack;
using Intersect.Network.Packets.Localization;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class LocalizedTextPacket : IntersectPacket
{
    public LocalizedTextPacket()
    {
    }

    public LocalizedTextPacket(string language, List<LocalizedTextEntry> entries)
    {
        Language = language;
        Entries = entries;
    }

    [Key(0)]
    public string Language { get; set; }

    [Key(1)]
    public List<LocalizedTextEntry> Entries { get; set; }
}
