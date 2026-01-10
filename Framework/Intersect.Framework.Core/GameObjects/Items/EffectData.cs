using Microsoft.EntityFrameworkCore;

namespace Intersect.Framework.Core.GameObjects.Items;

public readonly record struct EffectValue(int Percentage, int Flat)
{
    public int GetPrimaryValue(bool preferFlat = false)
    {
        return Percentage;
    }

    public EffectValue Add(EffectValue other)
    {
        return new EffectValue(Percentage + other.Percentage, 0);
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
        FlatAmount = 0;
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

    public EffectData Clone()
    {
        return new EffectData
        {
            Type = Type,
            Percentage = Percentage,
            FlatAmount = FlatAmount,
            IsFlat = IsFlat,
            IsPassive = IsPassive,
            Stacking = Stacking,
        };
    }

    public ItemEffect Type { get; set; }

    public int Percentage { get; set; }

    public int FlatAmount { get; set; }

    public bool IsFlat { get; set; }

    public bool IsPassive { get; set; }

    public EffectStacking Stacking { get; set; }

    public static bool SupportsFlatAndPercentage(ItemEffect effect)
    {
        return false;
    }

    public int GetValue()
    {
        return Percentage;
    }

    public EffectValue GetValues()
    {
        return new EffectValue(Percentage, 0);
    }
}
