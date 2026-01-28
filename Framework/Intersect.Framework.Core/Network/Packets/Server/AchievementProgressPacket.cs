using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class AchievementProgressPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public AchievementProgressPacket()
    {
    }

    public AchievementProgressPacket(Dictionary<Guid, string?> achievements)
    {
        Achievements = achievements;
    }

    [Key(0)]
    public Dictionary<Guid, string?> Achievements { get; set; } = new();
}
