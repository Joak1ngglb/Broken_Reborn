using System;
using System.Collections.Generic;
using Intersect.Enums;
using Intersect.Framework.Core.Combat;
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
        var accuracy = CombatFormulaCalculator.CalculateAccuracyScore(
            attacker.Stat[(int)Enums.Stat.Agility].Value(),
            attacker.Stat[(int)Enums.Stat.Attack].Value(),
            attackerEffects.GetTotalEffectValue(ItemEffect.Accuracy)
        );

        var evasion = CombatFormulaCalculator.CalculateEvasionScore(
            defender.Stat[(int)Enums.Stat.Agility].Value(),
            defender.Stat[(int)Enums.Stat.Defense].Value(),
            defenderEffects.GetTotalEffectValue(ItemEffect.Evasion)
        );

        return CombatFormulaCalculator.CalculateHitChance(
            accuracy,
            evasion,
            Options.Instance.Combat.BaseHitChance,
            Options.Instance.Combat.MinHitChance,
            Options.Instance.Combat.MaxHitChance,
            Options.Instance.Combat.HitChanceSwingFactor
        );
    }

    public static int CalculateCriticalChance(
        int baseCritChance,
        CombatantEffects attackerEffects,
        CombatantEffects defenderEffects
    )
    {
        return CombatFormulaCalculator.CalculateCriticalChance(
            baseCritChance,
            attackerEffects.Entity.Stat[(int)Enums.Stat.Agility].Value(),
            Options.Instance.Combat.AgilityPerCritChance,
            attackerEffects.GetTotalEffectValue(ItemEffect.CriticalChance),
            defenderEffects.GetTotalEffectValue(ItemEffect.AntiCritChance)
        );
    }

    public static IReadOnlyDictionary<string, object>? BuildDefenseOverrides(
        DamageType damageType,
        Entity defender,
        CombatantEffects attackerEffects
    )
    {
        if (damageType != DamageType.Physical)
        {
            return null;
        }

        var penetration = attackerEffects.GetTotalEffectValue(ItemEffect.ArmorPenetration);
        if (penetration == 0)
        {
            return null;
        }

        var baseDefense = defender.Stat[(int)Enums.Stat.Defense].Value();

        if (baseDefense <= 0)
        {
            return null;
        }

        var effectiveDefense = Math.Max(0, baseDefense * (1 - penetration / 100f));

        return new Dictionary<string, object> { ["V_Defense"] = effectiveDefense };
    }

    public static double ApplyCriticalReduction(double criticalMultiplier, CombatantEffects defenderEffects)
    {
        return CombatFormulaCalculator.ApplyCriticalReduction(
            criticalMultiplier,
            defenderEffects.GetTotalEffectValue(ItemEffect.CriticalReduction)
        );
    }

    public static long ApplyFinalDamageReduction(long damage, CombatantEffects defenderEffects)
    {
        var reduction = defenderEffects.GetTotalEffectValue(ItemEffect.DamageReduction);
        var reduced = damage;

        if (reduction != 0)
        {
            reduced = (long)Math.Round(reduced * (1 - reduction / 100f));
        }

        return reduced;
    }

    public static long ApplyDamageModifier(long damage, CombatantEffects attackerEffects)
    {
        var isHeal = damage < 0;
        var modifier = isHeal ? attackerEffects.GetTotalEffectValue(ItemEffect.Cures) :
            attackerEffects.GetTotalEffectValue(ItemEffect.Damages);

        if (modifier == 0)
        {
            return damage;
        }

        var adjusted = Math.Round(damage * (100 + modifier) / 100d);

        return (long)adjusted;
    }

    public static long CalculateReflectDamage(long appliedDamage, CombatantEffects defenderEffects, bool allowReflection)
    {
        if (!allowReflection || appliedDamage <= 0)
        {
            return 0;
        }

        var reflect = defenderEffects.GetTotalEffectValue(ItemEffect.DamageReflect);
        if (reflect == 0)
        {
            return 0;
        }

        return (int)(long)Math.Round(appliedDamage * (reflect / 100f));
    }
}
