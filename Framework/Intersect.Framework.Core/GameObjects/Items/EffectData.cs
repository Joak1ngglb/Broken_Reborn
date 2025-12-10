using Microsoft.EntityFrameworkCore;

namespace Intersect.Framework.Core.GameObjects.Items;

public readonly record struct EffectValue(int Percentage, int Flat)
{
    public int GetPrimaryValue(bool preferFlat = false)
    {
        if (preferFlat || Percentage == 0)
        {
            return Flat;
        }

        return Percentage;
    }

    public EffectValue Add(EffectValue other)
    {
        return new EffectValue(Percentage + other.Percentage, Flat + other.Flat);
    }
}

[Owned]
public partial class EffectData
{
    public EffectData()
    {
        Type = default;
        Percentage = default;
        IsPassive = true;
        Stacking = EffectStacking.Stack;
        FlatAmount = default;
        IsFlat = false;
    }

    public EffectData(
        ItemEffect type,
        int percentage,
        bool isPassive = true,
        EffectStacking stacking = EffectStacking.Stack,
        int flatAmount = 0,
        bool isFlat = false
    )
    {
        Type = type;
        Percentage = percentage;
        IsPassive = isPassive;
        Stacking = stacking;
        FlatAmount = flatAmount;
        IsFlat = isFlat;
    }

    public ItemEffect Type { get; set; }

    public int Percentage { get; set; }

    public int FlatAmount { get; set; }

    public bool IsFlat { get; set; }

    public bool IsPassive { get; set; }

    public EffectStacking Stacking { get; set; }

    public static bool SupportsFlatAndPercentage(ItemEffect effect)
    {
        return effect is ItemEffect.AntiCritChance
            or ItemEffect.ArmorPenetration
            or ItemEffect.DamageReduction
            or ItemEffect.DamageReflect;
    }

    public int GetValue()
    {
        return GetValues().GetPrimaryValue(IsFlat);
    }

    public EffectValue GetValues()
    {
        if (SupportsFlatAndPercentage(Type))
        {
            return new EffectValue(Percentage, FlatAmount);
        }

        return IsFlat ? new EffectValue(0, FlatAmount) : new EffectValue(Percentage, 0);
    }
}
