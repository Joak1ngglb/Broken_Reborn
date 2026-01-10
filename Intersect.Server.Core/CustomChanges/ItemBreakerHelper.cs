using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using System;
using System.Collections.Generic;
using System.Linq;

public static class ItemBreakHelper
{
    // Lista global de todas las runas
    private static List<ItemDescriptor> AllRunes = new();

    private const int StatThreshold = 10;
    private const int VitalThreshold = 20;
    private const int EffectPercentThreshold = 5;

    /// <summary>
    /// Debe llamarse una vez al arrancar el servidor, tras cargar los ItemDescriptor.
    /// </summary>
    public static void InitializeRunes()
    {
        AllRunes = ItemDescriptor.Lookup.Values
            .OfType<ItemDescriptor>()
            .Where(d =>
                d.ItemType == ItemType.Resource &&
                d.Subtype == "Rune" &&
                HasRuneTarget(d)
            )
            .ToList();
    }

    public static List<ItemDescriptor> CalculateRunesFromItem(ItemDescriptor item, ItemProperties? props)
    {
        var result = new List<ItemDescriptor>();

        int rarity = item.Rarity;
        int multiplier = 1 + rarity;

        // --- Procesar TODOS los Stats ---
        foreach (Stat stat in Enum.GetValues<Stat>())
        {
            int idx = (int)stat;
            int baseVal = item.StatsGiven[idx];
            int flatMod = props?.StatModifiers[idx] ?? 0;
            int percentMod = item.PercentageStatsGiven[idx];

            int totalStat = baseVal + flatMod;
            totalStat += (int)Math.Floor(totalStat * (percentMod / 100f));

            if (totalStat <= 0) continue;

            var pool = GetRunePool(stat, rarity);
            AddRunesFromValue(result, pool, totalStat, StatThreshold, multiplier);
        }

        // --- Procesar TODOS los Vitals ---
        foreach (Vital vital in Enum.GetValues<Vital>())
        {
            int idx = (int)vital;
            int baseVal = (int)item.VitalsGiven[idx];
            int flatMod = props?.VitalModifiers[idx] ?? 0;

            int totalVital = baseVal + flatMod;
            if (totalVital <= 0) continue;

            var pool = GetRunePool(vital, rarity);
            AddRunesFromValue(result, pool, (int)totalVital, VitalThreshold, multiplier);
        }

        // --- Procesar TODOS los efectos ---
        foreach (var kvp in AggregateEffects(item, props))
        {
            var effect = kvp.Key;
            var totalEffect = kvp.Value;

            if (totalEffect <= 0)
            {
                continue;
            }

            var pool = GetRunePool(effect, rarity);
            AddRunesFromValue(result, pool, totalEffect, EffectPercentThreshold, multiplier);
        }

        return result;
    }

    private static List<ItemDescriptor> GetRunePool(Stat stat, int rarity)
    {
        return AllRunes
            .Where(r =>
                r.TargetStat == stat &&
                IsRuneAllowedForRarity(r, rarity, false)
            )
            .OrderBy(r => r.AmountModifier)
            .ToList();
    }

    private static List<ItemDescriptor> GetRunePool(Vital vital, int rarity)
    {
        return AllRunes
            .Where(r =>
                r.TargetVital == vital &&
                IsRuneAllowedForRarity(r, rarity, true)
            )
            .OrderBy(r => r.AmountModifier)
            .ToList();
    }

    private static List<ItemDescriptor> GetRunePool(ItemEffect effect, int rarity)
    {
        return AllRunes
            .Where(r =>
                r.TargetEffect == effect &&
                IsRuneAllowedForRarity(r, rarity, false, true)
            )
            .OrderBy(r => r.AmountModifier)
            .ToList();
    }

    private static void AddRunesFromValue(List<ItemDescriptor> acc, List<ItemDescriptor> pool, int totalValue, int threshold, int multiplier)
    {
        if (totalValue <= 0 || pool.Count == 0)
        {
            return;
        }

        int guaranteed = (totalValue / threshold) * multiplier;
        double extraP = (totalValue % threshold) / (double)threshold;

        for (int i = 0; i < guaranteed; i++)
        {
            acc.Add(RandomRune(pool));
        }

        if (Random.Shared.NextDouble() < extraP)
        {
            acc.Add(RandomRune(pool));
        }
    }

    private static bool IsRuneAllowedForRarity(
        ItemDescriptor rune,
        int rarity,
        bool isVitalRune,
        bool isEffectRune = false
    )
    {
        var amount = Math.Abs(rune.AmountModifier);
        if (amount == 0)
        {
            return false;
        }

        if (isEffectRune)
        {
            var allowance = rarity switch
            {
                <= 0 => 3,
                1 => 5,
                2 => 7,
                3 => 10,
                4 => 13,
                _ => 16,
            };

            return amount <= allowance;
        }

        if (!isVitalRune && rune.AmountModifier > 3 && rarity < 3)
        {
            return false;
        }

        return true;
    }

    private static ItemDescriptor RandomRune(List<ItemDescriptor> runes)
    {
        var idx = Random.Shared.Next(runes.Count);
        return runes[idx];
    }

    private static bool HasRuneTarget(ItemDescriptor descriptor)
    {
        return descriptor.TargetStat >= 0 ||
               descriptor.TargetVital >= 0 ||
               descriptor.TargetEffect != ItemEffect.None;
    }

    private static Dictionary<ItemEffect, int> AggregateEffects(ItemDescriptor item, ItemProperties? props)
    {
        var totals = new Dictionary<ItemEffect, int>();

        void AddEffect(EffectData effect)
        {
            if (!effect.IsPassive || effect.Type == ItemEffect.None)
            {
                return;
            }

            totals.TryGetValue(effect.Type, out var current);
            current += effect.Percentage;
            totals[effect.Type] = current;
        }

        foreach (var effect in item.Effects)
        {
            AddEffect(effect);
        }

        if (props?.EffectModifiers != null)
        {
            foreach (var effect in props.EffectModifiers.Values)
            {
                AddEffect(effect);
            }
        }

        return totals;
    }
}
