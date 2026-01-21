using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class PlayerRespawnPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public PlayerRespawnPacket()
    {
    }

    public PlayerRespawnPacket(Guid playerId)
    {
        PlayerId = playerId;
    }

    [Key(0)]
    public Guid PlayerId { get; set; }
}
