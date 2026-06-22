using Intersect.Enums;
using Intersect.GameObjects.Ranges;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace Intersect.Framework.Core.GameObjects.Items;

public partial class EquipmentProperties
{
    [JsonIgnore]
    public ItemRange VitalRange_Health
    {
        get => VitalRanges.TryGetValue(Vital.Health, out var range) ? range : VitalRange_Health = new ItemRange();
        set => VitalRanges[Vital.Health] = value;
    }

    [JsonIgnore]
    public ItemRange VitalRange_Mana
    {
        get => VitalRanges.TryGetValue(Vital.Mana, out var range) ? range : VitalRange_Mana = new ItemRange();
        set => VitalRanges[Vital.Mana] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_CooldownReduction
    {
        get => EffectRanges.TryGetValue(ItemEffect.CooldownReduction, out var range)
            ? range
            : EffectRange_CooldownReduction = new ItemRange();
        set => EffectRanges[ItemEffect.CooldownReduction] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Lifesteal
    {
        get => EffectRanges.TryGetValue(ItemEffect.Lifesteal, out var range)
            ? range
            : EffectRange_Lifesteal = new ItemRange();
        set => EffectRanges[ItemEffect.Lifesteal] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Tenacity
    {
        get => EffectRanges.TryGetValue(ItemEffect.Tenacity, out var range)
            ? range
            : EffectRange_Tenacity = new ItemRange();
        set => EffectRanges[ItemEffect.Tenacity] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Luck
    {
        get => EffectRanges.TryGetValue(ItemEffect.Luck, out var range)
            ? range
            : EffectRange_Luck = new ItemRange();
        set => EffectRanges[ItemEffect.Luck] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_EXP
    {
        get => EffectRanges.TryGetValue(ItemEffect.EXP, out var range)
            ? range
            : EffectRange_EXP = new ItemRange();
        set => EffectRanges[ItemEffect.EXP] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Manasteal
    {
        get => EffectRanges.TryGetValue(ItemEffect.Manasteal, out var range)
            ? range
            : EffectRange_Manasteal = new ItemRange();
        set => EffectRanges[ItemEffect.Manasteal] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Accuracy
    {
        get => EffectRanges.TryGetValue(ItemEffect.Accuracy, out var range)
            ? range
            : EffectRange_Accuracy = new ItemRange();
        set => EffectRanges[ItemEffect.Accuracy] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Evasion
    {
        get => EffectRanges.TryGetValue(ItemEffect.Evasion, out var range)
            ? range
            : EffectRange_Evasion = new ItemRange();
        set => EffectRanges[ItemEffect.Evasion] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_CriticalChance
    {
        get => EffectRanges.TryGetValue(ItemEffect.CriticalChance, out var range)
            ? range
            : EffectRange_CriticalChance = new ItemRange();
        set => EffectRanges[ItemEffect.CriticalChance] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_AntiCritChance
    {
        get => EffectRanges.TryGetValue(ItemEffect.AntiCritChance, out var range)
            ? range
            : EffectRange_AntiCritChance = new ItemRange();
        set => EffectRanges[ItemEffect.AntiCritChance] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_ArmorPenetration
    {
        get => EffectRanges.TryGetValue(ItemEffect.ArmorPenetration, out var range)
            ? range
            : EffectRange_ArmorPenetration = new ItemRange();
        set => EffectRanges[ItemEffect.ArmorPenetration] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_DamageReduction
    {
        get => EffectRanges.TryGetValue(ItemEffect.DamageReduction, out var range)
            ? range
            : EffectRange_DamageReduction = new ItemRange();
        set => EffectRanges[ItemEffect.DamageReduction] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_DamageReflect
    {
        get => EffectRanges.TryGetValue(ItemEffect.DamageReflect, out var range)
            ? range
            : EffectRange_DamageReflect = new ItemRange();
        set => EffectRanges[ItemEffect.DamageReflect] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Damages
    {
        get => EffectRanges.TryGetValue(ItemEffect.Damages, out var range)
            ? range
            : EffectRange_Damages = new ItemRange();
        set => EffectRanges[ItemEffect.Damages] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_Cures
    {
        get => EffectRanges.TryGetValue(ItemEffect.Cures, out var range)
            ? range
            : EffectRange_Cures = new ItemRange();
        set => EffectRanges[ItemEffect.Cures] = value;
    }

    [JsonIgnore]
    public ItemRange EffectRange_CriticalReduction
    {
        get => EffectRanges.TryGetValue(ItemEffect.CriticalReduction, out var range)
            ? range
            : EffectRange_CriticalReduction = new ItemRange();
        set => EffectRanges[ItemEffect.CriticalReduction] = value;
    }
}
