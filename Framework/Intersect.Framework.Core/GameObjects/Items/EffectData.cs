using Microsoft.EntityFrameworkCore;

namespace Intersect.Framework.Core.GameObjects.Items;

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

    public int GetValue()
    {
        return IsFlat ? FlatAmount : Percentage;
    }
}