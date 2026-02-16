using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.NPCs;

public enum BossTriggerConditionType
{
    HealthPercentAtOrBelow = 0,
    StatusApplied = 1,
    CombatTimeAtOrAbove = 2,
}

public enum BossActionType
{
    CastSpell = 0,
    MoveToPosition = 1,
    SummonAdds = 2,
    ChangeTarget = 3,
    Enrage = 4,
    CastSpellFromList = 5,
    SetInternalState = 6,
}

public enum BossConditionType
{
    SelfHealthPercent = 0,
    TargetHealthPercent = 1,
    StatusPresent = 2,
    DistanceToTarget = 3,
    TimeSinceLastCastMilliseconds = 4,
    InternalState = 5,
}

public enum BossComparisonType
{
    LessOrEqual = 0,
    GreaterOrEqual = 1,
    BetweenInclusive = 2,
}

public enum BossStatusTargetType
{
    Self = 0,
    CurrentTarget = 1,
}

public enum BossTargetSelectionType
{
    HighestThreat = 0,
    Tank = 1,
    Healer = 2,
}

public class BossBehaviorProfile
{
    public const int LatestVersion = 1;

    public int Version { get; set; } = LatestVersion;

    public int EvaluationIntervalMs { get; set; } = 250;

    public int GlobalCooldownMs { get; set; } = 1000;

    public List<BossPhase> Phases { get; set; } = [];
}

public class BossPhase
{
    public string Id { get; set; } = string.Empty;

    public int MinimumHealthPercent { get; set; } = 0;

    public int MaximumHealthPercent { get; set; } = 100;

    public int Priority { get; set; }

    public List<BossTrigger> Triggers { get; set; } = [];

    public List<BossAction> Actions { get; set; } = [];
}

public class BossTrigger
{
    public BossTriggerConditionType Condition { get; set; } = BossTriggerConditionType.HealthPercentAtOrBelow;

    public int ThresholdHealthPercent { get; set; }

    public string RequiredAppliedState { get; set; } = string.Empty;

    public int CombatTimeSeconds { get; set; }

    public int CooldownSeconds { get; set; }

    public int Priority { get; set; }

    public List<BossCondition> Conditions { get; set; } = [];

    public List<BossAction> Actions { get; set; } = [];
}

public class BossAction
{
    public BossActionType Action { get; set; } = BossActionType.CastSpell;

    public Guid SpellId { get; set; } = Guid.Empty;

    public List<Guid> SpellIds { get; set; } = [];

    public int DestinationX { get; set; }

    public int DestinationY { get; set; }

    public Guid SummonNpcId { get; set; } = Guid.Empty;

    public int SummonAmount { get; set; }

    public bool TargetHighestThreat { get; set; } = true;

    public int EnragePercentBonus { get; set; }

    public BossTargetSelectionType TargetSelection { get; set; } = BossTargetSelectionType.HighestThreat;

    public string InternalStateKey { get; set; } = string.Empty;

    public bool InternalStateValue { get; set; } = true;

    public int Priority { get; set; }

    public int CooldownMs { get; set; }
}

public class BossCondition
{
    public BossConditionType Type { get; set; } = BossConditionType.SelfHealthPercent;

    public BossComparisonType Comparison { get; set; } = BossComparisonType.LessOrEqual;

    public int Value { get; set; }

    public int MinimumValue { get; set; }

    public int MaximumValue { get; set; }

    public SpellEffect StatusEffect { get; set; } = SpellEffect.None;

    public BossStatusTargetType StatusTarget { get; set; } = BossStatusTargetType.Self;

    public string InternalStateKey { get; set; } = string.Empty;
}
