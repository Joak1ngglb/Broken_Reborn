using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public partial class PlayerStatCondition : Condition
{
    public override ConditionType Type { get; } = ConditionType.PlayerStat;

    public PlayerStatType Stat { get; set; }

    public VariableComparator Comparator { get; set; } = VariableComparator.Equal;

    public int Value { get; set; }
}
