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
}

public class BossBehaviorProfile
{
    public const int LatestVersion = 1;

    public int Version { get; set; } = LatestVersion;

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
}

public class BossAction
{
    public BossActionType Action { get; set; } = BossActionType.CastSpell;

    public Guid SpellId { get; set; } = Guid.Empty;

    public int DestinationX { get; set; }

    public int DestinationY { get; set; }

    public Guid SummonNpcId { get; set; } = Guid.Empty;

    public int SummonAmount { get; set; }

    public bool TargetHighestThreat { get; set; } = true;

    public int EnragePercentBonus { get; set; }
}
