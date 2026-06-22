using System;

namespace Intersect.Framework.Core.Combat;

public static class CombatFormulaCalculator
{
    private const double MinHitChance = 0.25d;
    private const double MaxHitChance = 0.98d;
    private const double BaseHitChance = 0.75d;
    private const double HitChanceSwingFactor = 0.20d;

    public static double CalculateAccuracyScore(int agility, int attack, int accuracyBonus)
    {
        return agility * 0.5d + attack * 0.3d + accuracyBonus;
    }

    public static double CalculateEvasionScore(int agility, int defense, int evasionBonus)
    {
        return agility * 0.7d + defense * 0.2d + evasionBonus;
    }

    public static double CalculateHitChance(double accuracyScore, double evasionScore)
    {
        return CalculateHitChance(
            accuracyScore,
            evasionScore,
            BaseHitChance,
            MinHitChance,
            MaxHitChance,
            HitChanceSwingFactor
        );
    }

    public static double CalculateHitChance(
        double accuracyScore,
        double evasionScore,
        double baseHitChance,
        double minHitChance,
        double maxHitChance,
        double hitChanceSwingFactor
    )
    {
        var statBalance = accuracyScore - evasionScore;
        var normalization = Math.Max(50d, accuracyScore + evasionScore);
        var hitChance = baseHitChance + hitChanceSwingFactor * statBalance / normalization;

        return Math.Clamp(hitChance, minHitChance, maxHitChance);
    }

    public static int CalculateAgilityCriticalContribution(int agility, int agilityPerCritChance)
    {
        return agility / Math.Max(1, agilityPerCritChance);
    }

    public static int CalculateCriticalChance(
        int baseCritChance,
        int agility,
        int agilityPerCritChance,
        int criticalChanceBonus,
        int antiCritChance
    )
    {
        var critChance = baseCritChance;
        critChance += CalculateAgilityCriticalContribution(agility, agilityPerCritChance);
        critChance += criticalChanceBonus;
        critChance -= antiCritChance;

        return Math.Max(0, critChance);
    }

    public static double ApplyCriticalReduction(double criticalMultiplier, int criticalReduction)
    {
        if (criticalMultiplier <= 1d || criticalReduction <= 0)
        {
            return Math.Max(1d, criticalMultiplier);
        }

        return Math.Max(1d, criticalMultiplier - criticalReduction / 100d);
    }
}
