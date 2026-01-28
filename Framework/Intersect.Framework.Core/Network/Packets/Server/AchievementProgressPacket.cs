using Intersect.Framework.Core.GameObjects.Achievements;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class AchievementProgressPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public AchievementProgressPacket()
    {
    }

    public AchievementProgressPacket(Dictionary<Guid, AchievementProgressDto> achievements)
    {
        Achievements = achievements;
    }

    [Key(0)]
    public Dictionary<Guid, AchievementProgressDto> Achievements { get; set; } = new();
}
