using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Items;

[MessagePackObject]
public partial class ItemProperties
{
    public ItemProperties()
    {
    }

    public ItemProperties(ItemProperties other)
    {
        if (other == default)
        {
            throw new ArgumentNullException(nameof(other));
        }

        EnchantmentLevel = other.EnchantmentLevel;
        BaseDamageModifier = other.BaseDamageModifier;
        Array.Copy(other.StatModifiers, StatModifiers, Enum.GetValues<Stat>().Length);
        Array.Copy(other.VitalModifiers, VitalModifiers, Enum.GetValues<Vital>().Length);
        EffectModifiers = other.EffectModifiers?.ToDictionary(
                kvp => kvp.Key,
                kvp => new EffectData
                {
                    Type = kvp.Value.Type,
                    Percentage = kvp.Value.Percentage,
                    FlatAmount = kvp.Value.FlatAmount,
                    IsFlat = kvp.Value.IsFlat,
                    IsPassive = kvp.Value.IsPassive,
                    Stacking = kvp.Value.Stacking
                }
            ) ?? new Dictionary<ItemEffect, EffectData>();

    }

    [Key(0)]
    public int[] StatModifiers { get; set; } = new int[Enum.GetValues<Stat>().Length];

    [Key(1)]
    public int EnchantmentLevel { get; set; }  // Nivel de encantamiento
    [Key(2)]
    public Dictionary<int, int[]> EnchantmentRolls { get; set; } = new Dictionary<int, int[]>();
    [Key(3)]
    public int[] VitalModifiers { get; set; } = new int[Enum.GetValues<Vital>().Length];
    [Key(4)]
    public int MageSink { get;  set; }
    [Key(5)]
    public int BaseDamageModifier { get; set; }
    [Key(6)]
    public Dictionary<ItemEffect, EffectData> EffectModifiers { get; set; } = new();
}
