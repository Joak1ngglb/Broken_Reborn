using System;
using Intersect.Enums;

namespace Intersect.Framework.Core.Localization;

public static class LocalizationEntityTypes
{
    public const string Animation = "Animation";
    public const string Class = "Class";
    public const string Crafts = "Crafts";
    public const string CraftTables = "CraftTables";
    public const string Event = "Event";
    public const string GuildVariable = "GuildVariable";
    public const string Item = "Item";
    public const string Map = "Map";
    public const string Npc = "Npc";
    public const string PlayerVariable = "PlayerVariable";
    public const string Projectile = "Projectile";
    public const string Quest = "Quest";
    public const string Resource = "Resource";
    public const string ServerVariable = "ServerVariable";
    public const string Sets = "Sets";
    public const string Shop = "Shop";
    public const string Spell = "Spell";
    public const string Tileset = "Tileset";
    public const string Time = "Time";
    public const string UserVariable = "UserVariable";

    public static bool IsKnown(string entityType) =>
        !string.IsNullOrWhiteSpace(entityType) &&
        TryFromGameObjectType(entityType, out _);

    public static string FromGameObjectType(GameObjectType type) =>
        type switch
        {

            GameObjectType.Animation => Animation,
            GameObjectType.Class => Class,
            GameObjectType.Crafts => Crafts,
            GameObjectType.CraftTables => CraftTables,
            GameObjectType.Event => Event,
            GameObjectType.GuildVariable => GuildVariable,
            GameObjectType.Item => Item,
            GameObjectType.Map => Map,
            GameObjectType.Npc => Npc,
            GameObjectType.PlayerVariable => PlayerVariable,
            GameObjectType.Projectile => Projectile,
            GameObjectType.Quest => Quest,
            GameObjectType.Resource => Resource,
            GameObjectType.ServerVariable => ServerVariable,
            GameObjectType.Sets => Sets,
            GameObjectType.Shop => Shop,
            GameObjectType.Spell => Spell,
            GameObjectType.Tileset => Tileset,
            GameObjectType.Time => Time,
         
            GameObjectType.UserVariable => UserVariable,
            _ => type.ToString(),
        };

    public static bool TryFromGameObjectType(string entityType, out GameObjectType gameObjectType) =>
        Enum.TryParse(entityType, ignoreCase: false, out gameObjectType) &&
        Enum.IsDefined(gameObjectType);
}
