using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Collections;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public sealed partial class AchievementDescriptor : DatabaseObject<AchievementDescriptor>, IFolderable
{
    public AchievementDescriptor()
    {
        Name = "New Achievement";
    }

    [JsonConstructor]
    public AchievementDescriptor(Guid id) : base(id)
    {
        Name = "New Achievement";
    }

    public string Description { get; set; } = string.Empty;

    public AchievementCategory Category { get; set; } = AchievementCategory.Exploracion;

    public AchievementDifficulty Difficulty { get; set; } = AchievementDifficulty.Natural;

    [Column("CompletionMode")]
    public AchievementCompletionMode CompletionMode { get; set; } = AchievementCompletionMode.OrListsAndConditions;

    public int OrderValue { get; set; }

    [Column("Requirements")]
    [JsonIgnore]
    public string RequirementsJson
    {
        get => Requirements.Data();
        set => Requirements.Load(value);
    }

    [NotMapped]
    public ConditionLists Requirements { get; set; } = new();

    [Column("Rewards")]
    [JsonIgnore]
    public string RewardsJson
    {
        get => JsonConvert.SerializeObject(new
        {
            Rewards.Experience,
            Rewards.Currency,
            Rewards.Resources,
            Rewards.TitleIds,
        });
        set => Rewards = string.IsNullOrWhiteSpace(value)
            ? new AchievementRewards()
            : JsonConvert.DeserializeObject<AchievementRewards>(value) ?? new AchievementRewards();
    }

    [NotMapped]
    public AchievementRewards Rewards { get; set; } = new();

    [Column("MetaAchievementIds")]
    [JsonIgnore]
    public string MetaAchievementIdsJson
    {
        get => JsonConvert.SerializeObject(MetaAchievementIds);
        set => MetaAchievementIds = string.IsNullOrWhiteSpace(value)
            ? []
            : JsonConvert.DeserializeObject<List<Guid>>(value) ?? [];
    }

    [NotMapped]
    public List<Guid> MetaAchievementIds { get; set; } = [];

    public string? Folder { get; set; } = string.Empty;

    public static DatabaseObjectLookup AchievementLookup => Lookup;
}

public sealed class AchievementRewards
{
    public long Experience { get; set; }

    public long Currency { get; set; }

    public Dictionary<Guid, int> Resources { get; set; } = [];

    public List<Guid> TitleIds { get; set; } = [];
}
