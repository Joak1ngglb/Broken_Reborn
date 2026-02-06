using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Collections;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public sealed partial class AchievementDescriptor : DatabaseObject<AchievementDescriptor>, IFolderable
{
    private readonly HashSet<Guid> _itemRequirementIds = [];
    private readonly HashSet<Guid> _mapRequirementIds = [];
    private readonly HashSet<Guid> _questRequirementIds = [];
    private readonly HashSet<MapZone> _zoneRequirementTypes = [];
    private bool _hasItemRequirement;
    private bool _hasMapRequirement;
    private bool _hasQuestRequirement;
    private bool _requirementMetadataDirty = true;
    private ConditionLists _requirements = new();

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

    public AchievementCategory Category { get; set; } = AchievementCategory.Exploration;

    public AchievementDifficulty Difficulty { get; set; } = AchievementDifficulty.Natural;

    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// The database compatible version of <see cref="Color"/>
    /// </summary>
    [Column("Color")]
    [JsonIgnore]
    public string JsonColor
    {
        get => JsonConvert.SerializeObject(Color);
        set => Color = !string.IsNullOrWhiteSpace(value) ? JsonConvert.DeserializeObject<Color>(value) : Color.White;
    }

    /// <summary>
    /// Defines the ARGB color settings for this Achievement.
    /// </summary>
    [NotMapped]
    public Color Color { get; set; } = Color.White;

    [Column("CompletionMode")]
    public AchievementCompletionMode CompletionMode { get; set; } = AchievementCompletionMode.OrListsAndConditions;

    public int OrderValue { get; set; }

    [Column("Requirements")]
    [JsonIgnore]
    public string RequirementsJson
    {
        get => Requirements.Data();
        set
        {
            Requirements.Load(value);
            RebuildRequirementMetadata();
        }
    }

    [NotMapped]
    public ConditionLists Requirements
    {
        get => _requirements;
        set
        {
            _requirements = value ?? new ConditionLists();
            InvalidateRequirementMetadata();
        }
    }

    [NotMapped]
    public IReadOnlyCollection<Guid> QuestRequirementIds => _questRequirementIds;

    [NotMapped]
    public IReadOnlyCollection<Guid> ItemRequirementIds => _itemRequirementIds;

    [NotMapped]
    public IReadOnlyCollection<Guid> MapRequirementIds => _mapRequirementIds;

    [NotMapped]
    public IReadOnlyCollection<MapZone> ZoneRequirementTypes => _zoneRequirementTypes;

    [NotMapped]
    public bool HasQuestRequirement => _hasQuestRequirement;

    [NotMapped]
    public bool HasItemRequirement => _hasItemRequirement;

    [NotMapped]
    public bool HasMapRequirement => _hasMapRequirement;

    [Column("Rewards")]
    [JsonIgnore]
    public string RewardsJson
    {
        get => JsonConvert.SerializeObject(Rewards);
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

    public void InvalidateRequirementMetadata()
    {
        _requirementMetadataDirty = true;
    }

    public void EnsureRequirementMetadata()
    {
        if (_requirementMetadataDirty)
        {
            RebuildRequirementMetadata();
        }
    }

    public void RebuildRequirementMetadata()
    {
        _questRequirementIds.Clear();
        _itemRequirementIds.Clear();
        _mapRequirementIds.Clear();
        _zoneRequirementTypes.Clear();
        _hasQuestRequirement = false;
        _hasItemRequirement = false;
        _hasMapRequirement = false;

        foreach (var list in Requirements.Lists)
        {
            foreach (var condition in list.Conditions)
            {
                switch (condition)
                {
                    case QuestCompletedCondition questCondition:
                        _hasQuestRequirement = true;
                        _questRequirementIds.Add(questCondition.QuestId);
                        break;
                    case HasItemCondition itemCondition:
                        _hasItemRequirement = true;
                        _itemRequirementIds.Add(itemCondition.ItemId);
                        break;
                    case MapIsCondition mapCondition:
                        _hasMapRequirement = true;
                        _mapRequirementIds.Add(mapCondition.MapId);
                        break;
                    case MapZoneTypeIs zoneCondition:
                        _hasMapRequirement = true;
                        _zoneRequirementTypes.Add(zoneCondition.ZoneType);
                        break;
                }
            }
        }

        _requirementMetadataDirty = false;
    }
}

public sealed class AchievementRewards
{
    public long Experience { get; set; }

    public long Currency { get; set; }

    [JsonProperty("Items")]
    public Dictionary<Guid, int> Items { get; set; } = [];

    [JsonProperty("Resources")]
    private Dictionary<Guid, int>? LegacyResources
    {
        set => Items = value ?? [];
    }

    public List<Guid> TitleIds { get; set; } = [];
}
