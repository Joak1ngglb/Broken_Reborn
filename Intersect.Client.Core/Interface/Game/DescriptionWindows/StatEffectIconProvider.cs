using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Client.Interface.Game.DescriptionWindows;

public static class StatEffectIconProvider
{
    public static IGameTexture? GetIconTexture(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        var contentManager = GameContentManager.Current;
        return contentManager.GetTexture(TextureType.Misc, iconName)
               ?? contentManager.GetTexture(TextureType.Gui, iconName);
    }

    public static string? GetIconForStat(Stat stat)
    {
        return stat switch
        {
            Stat.Attack => "stat_attack.png",
            Stat.Intelligence => "stat_intelligence.png",
            Stat.Defense => "stat_defense.png",
            Stat.Vitality => "stat_vitality.png",
            Stat.Speed => "stat_speed.png",
            Stat.Agility => "stat_agility.png",
            Stat.Willpower => "stat_intelligence.png",
            _ => null,
        };
    }

    public static string? GetIconForVital(Vital vital)
    {
        return vital switch
        {
            Vital.Health => "vital_health.png",
            Vital.Mana => "vital_mana.png",
            _ => null,
        };
    }

    public static string? GetIconForItemEffect(ItemEffect effect)
    {
        return effect switch
        {
            ItemEffect.CooldownReduction => "effect_cooldown_reduction.png",
            ItemEffect.Lifesteal => "effect_lifesteal.png",
            ItemEffect.Tenacity => "effect_tenacity.png",
            ItemEffect.Luck => "effect_luck.png",
            ItemEffect.EXP => "effect_exp.png",
            ItemEffect.Manasteal => "effect_manasteal.png",
            ItemEffect.Accuracy => "effect_accuracy.png",
            ItemEffect.Evasion => "effect_evasion.png",
            ItemEffect.CriticalChance => "effect_critical_chance.png",
            ItemEffect.AntiCritChance => "effect_anti_crit.png",
            ItemEffect.ArmorPenetration => "effect_armor_penetration.png",
            ItemEffect.DamageReduction => "effect_damage_reduction.png",
            ItemEffect.DamageReflect => "effect_damage_reflect.png",
            ItemEffect.Damages => "effect_damage.png",
            ItemEffect.Cures => "effect_cure.png",
            ItemEffect.CriticalReduction => "effect_damage_reduction.png",
            _ => null,
        };
    }
}
