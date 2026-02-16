using System;

namespace Intersect.Framework.Core.Combat;

public static class CombatFormulaCalculator
{
    private const double MinHitChance = 0.1d;
    private const double MaxHitChance = 0.98d;
    private const double BaseHitChance = 0.7d;
    private const double HitChanceSwingFactor = 0.3d;

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
        var statBalance = accuracyScore - evasionScore;
        var normalization = Math.Max(50d, accuracyScore + evasionScore);
        var hitChance = BaseHitChance + HitChanceSwingFactor * statBalance / normalization;

        return Math.Clamp(hitChance, MinHitChance, MaxHitChance);
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
}
