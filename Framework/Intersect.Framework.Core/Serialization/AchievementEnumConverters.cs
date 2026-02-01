using System;
using Intersect.Enums;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.Serialization;

public sealed class AchievementCategoryConverter : JsonConverter<AchievementCategory>
{
    public override AchievementCategory ReadJson(
        JsonReader reader,
        Type objectType,
        AchievementCategory existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            return (AchievementCategory)Convert.ToInt32(reader.Value);
        }

        if (reader.TokenType != JsonToken.String || reader.Value is not string value)
        {
            return existingValue;
        }

        return value switch
        {
            nameof(AchievementCategory.Dungeons) or "Mazmorras" => AchievementCategory.Dungeons,
            nameof(AchievementCategory.Exploration) or "Exploracion" => AchievementCategory.Exploration,
            nameof(AchievementCategory.Monsters) or "Monstruos" => AchievementCategory.Monsters,
            nameof(AchievementCategory.Quests) or "Misiones" => AchievementCategory.Quests,
            nameof(AchievementCategory.Professions) or "Oficios" => AchievementCategory.Professions,
            nameof(AchievementCategory.Events) or "Eventos" => AchievementCategory.Events,
            _ when Enum.TryParse<AchievementCategory>(value, true, out var parsed) => parsed,
            _ => existingValue
        };
    }

    public override void WriteJson(JsonWriter writer, AchievementCategory value, JsonSerializer serializer)
    {
        writer.WriteValue((int)value);
    }
}

public sealed class AchievementDifficultyConverter : JsonConverter<AchievementDifficulty>
{
    public override AchievementDifficulty ReadJson(
        JsonReader reader,
        Type objectType,
        AchievementDifficulty existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            return (AchievementDifficulty)Convert.ToInt32(reader.Value);
        }

        if (reader.TokenType != JsonToken.String || reader.Value is not string value)
        {
            return existingValue;
        }

        return value switch
        {
            nameof(AchievementDifficulty.Discovery) or "Descubrimiento" => AchievementDifficulty.Discovery,
            nameof(AchievementDifficulty.Natural) or "Natural" => AchievementDifficulty.Natural,
            nameof(AchievementDifficulty.Epic) or "Epico" => AchievementDifficulty.Epic,
            nameof(AchievementDifficulty.Meta) or "Meta" => AchievementDifficulty.Meta,
            _ when Enum.TryParse<AchievementDifficulty>(value, true, out var parsed) => parsed,
            _ => existingValue
        };
    }

    public override void WriteJson(JsonWriter writer, AchievementDifficulty value, JsonSerializer serializer)
    {
        writer.WriteValue((int)value);
    }
}
