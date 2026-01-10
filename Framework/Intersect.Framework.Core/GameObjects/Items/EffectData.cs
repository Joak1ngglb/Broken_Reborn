using MessagePack;
using Microsoft.EntityFrameworkCore;

namespace Intersect.Framework.Core.GameObjects.Items;

[MessagePackObject]
[Owned]
public partial class EffectData
{
    public EffectData()
    {
        Type = default;
        Percentage = default;
        IsPassive = true;
        Stacking = EffectStacking.Stack;
    }

    public EffectData(
        ItemEffect type,
        int percentage,
        bool isPassive = true,
        EffectStacking stacking = EffectStacking.Stack
    )
    {
        Type = type;
        Percentage = percentage;
        IsPassive = isPassive;
        Stacking = stacking;
    }

    [Key(0)]
    public ItemEffect Type { get; set; }

    [Key(1)]
    public int Percentage { get; set; }

    [Key(2)]
    public bool IsPassive { get; set; }

    [Key(3)]
    public EffectStacking Stacking { get; set; }
}
