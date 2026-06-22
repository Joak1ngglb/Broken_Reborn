using Intersect;

﻿namespace Intersect.Config;

public partial class CombatOptions
{
    public float DamageScreenFlashIntensity { get; set; } = 0.18f;

    public int DamageScreenFlashDurationMs { get; set; } = 130;

    public Color DamageScreenFlashColor { get; set; } = new(255, 255, 80, 80);

    public float HealScreenFlashIntensity { get; set; } = 0.12f;

    public int HealScreenFlashDurationMs { get; set; } = 110;

    public Color HealScreenFlashColor { get; set; } = new(255, 80, 255, 120);

    public float CriticalScreenFlashIntensity { get; set; } = 0.28f;

    public int CriticalScreenFlashDurationMs { get; set; } = 170;

    public Color CriticalScreenFlashColor { get; set; } = new(255, 255, 240, 120);

    public float DamageScreenShakeAmount { get; set; } = 2.5f;

    public float HealScreenShakeAmount { get; set; } = 1.2f;

    public float CriticalScreenShakeAmount { get; set; } = 4.2f;

    public int ScreenShakeDurationMs { get; set; } = 140;

    public float MaxScreenShakeAmount { get; set; } = 8f;

    public string DamageCombatEffectSound { get; set; } = string.Empty;

    public string HealCombatEffectSound { get; set; } = string.Empty;

    public string CriticalCombatEffectSound { get; set; } = string.Empty;

    public Color DamageEntityFlashColor { get; set; } = new(255, 255, 90, 90);

    public Color HealEntityFlashColor { get; set; } = new(255, 90, 255, 120);

    public Color CriticalEntityFlashColor { get; set; } = new(255, 255, 230, 120);

    public float EntityFlashIntensity { get; set; } = 0.45f;

    public int EntityFlashDurationMs { get; set; } = 120;

    public int BlockingSlow { get; set; } = 30; //Slow when moving with a shield. Default 30%

    public int CombatTime { get; set; } = 10000; //10 seconds

    public int MaxAttackRate { get; set; } = 200; //5 attacks per second

    public int MaxDashSpeed { get; set; } = 200;

    /// <summary>
    /// Number of agility points required to gain 1% critical chance.
    /// </summary>
    public int AgilityPerCritChance { get; set; } = 20;

    /// <summary>
    /// Baseline hit chance when accuracy and evasion are evenly matched.
    /// </summary>
    public double BaseHitChance { get; set; } = 0.75d;

    /// <summary>
    /// Lowest allowed hit chance after accuracy and evasion modifiers are applied.
    /// </summary>
    public double MinHitChance { get; set; } = 0.25d;

    /// <summary>
    /// Highest allowed hit chance after accuracy and evasion modifiers are applied.
    /// </summary>
    public double MaxHitChance { get; set; } = 0.98d;

    /// <summary>
    /// How strongly the accuracy vs evasion delta shifts the final hit chance.
    /// </summary>
    public double HitChanceSwingFactor { get; set; } = 0.20d;

    /// <summary>
    /// Allowed distance to target party members when using quick target keys.
    /// </summary>
    public int PartyTargetDistance { get; set; } = 20;

    public int MinAttackRate { get; set; } = 500; //2 attacks per second

    public string PlayerDeathAnimationId { get; set; } = "95f735e1-0c32-46a8-9a9a-b472a7a3fedd";

    //Combat
    public int RegenTime { get; set; } = 3000; //3 seconds

    public bool EnableCombatChatMessages { get; set; } = false; // Enables or disables combat chat messages.

    //Spells

    /// <summary>
    /// If enabled this allows spell casts to stop/be canceled if the player tries to move around (WASD)
    /// </summary>
    public bool MovementCancelsCast { get; set; } = false;

    // Cooldowns

    /// <summary>
    /// Configures whether cooldowns within cooldown groups should match.
    /// </summary>
    public bool MatchGroupCooldowns { get; set; } = true;

    /// <summary>
    /// Only used when <seealso cref="MatchGroupCooldowns"/> is enabled!
    /// Configures whether cooldowns are being matched to the highest cooldown within a cooldown group when true, or are matched to the current item or spell being used when false.
    /// </summary>
    public bool MatchGroupCooldownHighest { get; set; } = true;

    /// <summary>
    /// Only used when <seealso cref="MatchGroupCooldowns"/> is enabled!
    /// Configures whether cooldown groups between items and spells are shared.
    /// </summary>
    public bool LinkSpellAndItemCooldowns { get; set; } = true;

    /// <summary>
    /// Configures whether or not using a spell or item should trigger a global cooldown.
    /// </summary>
    public bool EnableGlobalCooldowns { get; set; } = false;

    /// <summary>
    /// Configures the duration (in milliseconds) which the global cooldown lasts after each ability.
    /// Only used when <seealso cref="EnableGlobalCooldowns"/> is enabled!
    /// </summary>
    public int GlobalCooldownDuration { get; set; } = 1500;

    /// <summary>
    /// Configures the maximum distance a target is allowed to be from the player when auto targetting.
    /// </summary>
    public int MaxPlayerAutoTargetRadius { get; set; } = 15;

    /// <summary>
    /// If enabled this allows regenerate vitals in combat
    /// </summary>
    public bool RegenVitalsInCombat { get; set; } = false;

    /// <summary>
    /// If enabled, healing can benefit from critical strike multipliers.
    /// </summary>
    public bool HealingCanCrit { get; set; } = true;

    /// <summary>
    /// If enabled, healing calculations ignore the defender's defensive stats.
    /// </summary>
    public bool HealingIgnoresDefense { get; set; }

    /// <summary>
    /// If enabled, reflected damage cannot trigger additional reflections.
    /// </summary>
    public bool PreventReflectChaining { get; set; } = true;

    /// <summary>
    /// If enabled, this allows entities to turn around while casting
    /// </summary>
    public bool EnableTurnAroundWhileCasting { get; set; } = false;

    /// <summary>
    /// If enabled, the target window will be shown to players whenever they target an entity
    /// </summary>
    public bool EnableTargetWindow { get; set; } = true;

    /// <summary>
    /// If enabled, this makes it so a player casting a friendly spell on a hostile target instead casts the spell upon themselves
    /// </summary>
    public bool EnableAutoSelfCastFriendlySpellsWhenTargetingHostile { get; set; } = false;

    /// <summary>
    /// If enabled, this allows players to cast friendly spells on players who aren't in their guild or party
    /// </summary>
    public bool EnableAllPlayersFriendlyInSafeZone { get; set; } = false;
}
