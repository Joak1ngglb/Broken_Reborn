using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class AchievementCompletedPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public AchievementCompletedPacket()
    {
    }

    public AchievementCompletedPacket(
        Guid achievementId,
        long experience,
        long currency,
        Dictionary<Guid, int> resources,
        List<Guid> titleIds,
        List<Guid> ornamentIds
    )
    {
        AchievementId = achievementId;
        Experience = experience;
        Currency = currency;
        Resources = resources;
        TitleIds = titleIds;
        OrnamentIds = ornamentIds;
    }

    [Key(0)]
    public Guid AchievementId { get; set; }

    [Key(1)]
    public long Experience { get; set; }

    [Key(2)]
    public long Currency { get; set; }

    [Key(3)]
    public Dictionary<Guid, int> Resources { get; set; } = new();

    [Key(4)]
    public List<Guid> TitleIds { get; set; } = new();

    [Key(5)]
    public List<Guid> OrnamentIds { get; set; } = new();
}
