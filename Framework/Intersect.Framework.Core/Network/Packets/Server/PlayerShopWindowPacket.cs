using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public class PlayerShopWindowPacket : IntersectPacket
{
    public PlayerShopWindowPacket(bool openCreator, bool closeCreator, bool closeBrowser)
    {
        OpenCreator = openCreator;
        CloseCreator = closeCreator;
        CloseBrowser = closeBrowser;
    }

    [Key(0)] public bool OpenCreator { get; set; }

    [Key(1)] public bool CloseCreator { get; set; }

    [Key(2)] public bool CloseBrowser { get; set; }
}
