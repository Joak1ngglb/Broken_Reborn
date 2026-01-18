using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class ClientLanguagePacket : IntersectPacket
{
    public ClientLanguagePacket()
    {
    }

    public ClientLanguagePacket(string language)
    {
        Language = language;
    }

    [Key(0)]
    public string Language { get; set; }
}
