using Intersect.Framework.Core.Serialization;
using Newtonsoft.Json;

namespace Intersect.Enums;

[JsonConverter(typeof(AchievementDifficultyConverter))]
public enum AchievementDifficulty
{
    Discovery,
    Natural,
    Epic,
    Meta,
}
