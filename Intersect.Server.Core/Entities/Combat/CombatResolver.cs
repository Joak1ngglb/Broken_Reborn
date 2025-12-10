using System;
using System.Collections.Generic;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Server.Entities;
using Intersect.Server.General;

namespace Intersect.Server.Entities.Combat;

public static class CombatResolver
{
    public static CombatEffectsSnapshot BuildEffects(Entity attacker, Entity defender)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);

        return new CombatEffectsSnapshot(
            new CombatantEffects(attacker, CombatEffectCollection.From(attacker.GetActiveEffects())),
            new CombatantEffects(defender, CombatEffectCollection.From(defender.GetActiveEffects()))
        );
    }

    public static double CalculateHitChance(
        Entity attacker,
        Entity defender,
        CombatantEffects attackerEffects,
        CombatantEffects defenderEffects
    )
    {
        const double minChance = 0.1d;
        const double maxChance = 0.98d;
        const double baseChance = 0.7d;
        const double swingFactor = 0.3d;

        var accuracy =
            attacker.Stat[(int)Enums.Stat.Agility].Value() * 0.5d +
            attacker.Stat[(int)Enums.Stat.Attack].Value() * 0.3d +
            attackerEffects.GetTotalEffectValue(ItemEffect.Accuracy).GetPrimaryValue();

        var evasion =
            defender.Stat[(int)Enums.Stat.Agility].Value() * 0.7d +
            defender.Stat[(int)Enums.Stat.Defense].Value() * 0.2d +
            defenderEffects.GetTotalEffectValue(ItemEffect.Evasion).GetPrimaryValue();

        var statBalance = accuracy - evasion;
        var normalization = Math.Max(50d, accuracy + evasion);
        var hitChance = baseChance + swingFactor * statBalance / normalization;

        return Math.Clamp(hitChance, minChance, maxChance);
    }

    public static int CalculateCriticalChance(
        int baseCritChance,
        CombatantEffects attackerEffects,
        CombatantEffects defenderEffects
    )
    {
        var critChance = baseCritChance;

        var agilityPerCrit = Math.Max(1, Options.Instance.Combat.AgilityPerCritChance);
        critChance += attackerEffects.Entity.Stat[(int)Enums.Stat.Agility].Value() / agilityPerCrit;
        critChance += attackerEffects.GetTotalEffectValue(ItemEffect.CriticalChance).GetPrimaryValue();

        var antiCrit = defenderEffects.GetTotalEffectValue(ItemEffect.AntiCritChance);
        critChance -= antiCrit.Percentage + antiCrit.Flat;

        return Math.Max(0, critChance);
    }

    public static IReadOnlyDictionary<string, object>? BuildDefenseOverrides(
        DamageType damageType,
        Entity defender,
        CombatantEffects attackerEffects
    )
    {
        var penetration = attackerEffects.GetTotalEffectValue(ItemEffect.ArmorPenetration);
        if (penetration.Percentage == 0 && penetration.Flat == 0)
        {
            return null;
        }

        var baseDefense = damageType switch
        {
            DamageType.Magic => defender.Stat[(int)Enums.Stat.Vitality].Value(),
            DamageType.Physical => defender.Stat[(int)Enums.Stat.Defense].Value(),
            _ => 0,
        };

        if (baseDefense <= 0)
        {
            return null;
        }

        var effectiveDefense = Math.Max(
            0,
            (baseDefense - penetration.Flat) * (1 - penetration.Percentage / 100f)
        );

        var parameter = damageType == DamageType.Magic ? "V_MagicResist" : "V_Defense";
        return new Dictionary<string, object> { [parameter] = effectiveDefense };
    }

    public static long ApplyFinalDamageReduction(long damage, CombatantEffects defenderEffects)
    {
        var reduction = defenderEffects.GetTotalEffectValue(ItemEffect.DamageReduction);
        var reduced = damage;

        if (reduction.Percentage != 0)
        {
            reduced = (long)Math.Round(reduced * (1 - reduction.Percentage / 100f));
        }

        if (reduction.Flat != 0)
        {
            reduced = Math.Max(0, reduced - reduction.Flat);
        }

        return reduced;
    }

    public static long ApplyDamageModifier(long damage, CombatantEffects attackerEffects)
    {
        var isHeal = damage < 0;
        var modifier = isHeal ? attackerEffects.GetTotalEffectValue(ItemEffect.Cures) :
            attackerEffects.GetTotalEffectValue(ItemEffect.Damages);

        if (modifier.Percentage == 0)
        {
            return damage;
        }

        var adjusted = Math.Round(damage * (100 + modifier.Percentage) / 100d);

        return (long)adjusted;
    }

    public static long CalculateReflectDamage(long appliedDamage, CombatantEffects defenderEffects, bool allowReflection)
    {
        if (!allowReflection || appliedDamage <= 0)
        {
            return 0;
        }

        var reflect = defenderEffects.GetTotalEffectValue(ItemEffect.DamageReflect);
        if (reflect.Percentage == 0 && reflect.Flat == 0)
        {
            return 0;
        }

        var reflectedDamage = reflect.Flat;
        if (reflect.Percentage != 0)
        {
            reflectedDamage += (int)(long)Math.Round(appliedDamage * (reflect.Percentage / 100f));
        }

        return reflectedDamage;
    }
}
