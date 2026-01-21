using Intersect.Enums;
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

    public PlayerRespawnPacket(Guid playerId, Guid mapId, int x, int y, long hp, long mp, Direction? direction = null)
    {
        PlayerId = playerId;
        MapId = mapId;
        X = x;
        Y = y;
        Hp = hp;
        Mp = mp;
        Direction = direction;
    }

    [Key(0)]
    public Guid PlayerId { get; set; }

    [Key(1)]
    public Guid MapId { get; set; }

    [Key(2)]
    public int X { get; set; }

    [Key(3)]
    public int Y { get; set; }

    [Key(4)]
    public long Hp { get; set; }

    [Key(5)]
    public long Mp { get; set; }

    [Key(6)]
    public Direction? Direction { get; set; }
}
