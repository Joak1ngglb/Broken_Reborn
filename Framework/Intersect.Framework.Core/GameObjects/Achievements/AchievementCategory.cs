using Intersect.Framework.Core.Serialization;
using Newtonsoft.Json;

namespace Intersect.Enums;

[JsonConverter(typeof(AchievementCategoryConverter))]
public enum AchievementCategory
{
    Dungeons,
    Exploration,
    Monsters,
    Quests,
    Professions,
    Events,
}
