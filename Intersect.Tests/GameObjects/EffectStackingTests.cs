using System.Collections.Generic;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using NUnit.Framework;

namespace Intersect.Tests.GameObjects;

public class EffectStackingTests
{
    [Test]
    public void ApplyEffect_StacksDuplicates()
    {
        var bonuses = new Dictionary<ItemEffect, EffectValue>();
        var effect1 = new EffectData(ItemEffect.Luck, 10, true, EffectStacking.Stack);
        var effect2 = new EffectData(ItemEffect.Luck, 5, true, EffectStacking.Stack);
        bonuses.ApplyEffect(effect1);
        bonuses.ApplyEffect(effect2);
        Assert.That(bonuses[ItemEffect.Luck].Percentage, Is.EqualTo(15));
    }

    [Test]
    public void ApplyEffect_IgnoresDuplicates()
    {
        var bonuses = new Dictionary<ItemEffect, EffectValue>();
        var effect1 = new EffectData(ItemEffect.Luck, 10, true, EffectStacking.Ignore);
        var effect2 = new EffectData(ItemEffect.Luck, 5, true, EffectStacking.Ignore);
        bonuses.ApplyEffect(effect1);
        bonuses.ApplyEffect(effect2);
        Assert.That(bonuses[ItemEffect.Luck].Percentage, Is.EqualTo(10));
    }

    [Test]
    public void ApplyEffect_RenewsDuplicates()
    {
        var bonuses = new Dictionary<ItemEffect, EffectValue>();
        var effect1 = new EffectData(ItemEffect.Luck, 10, true, EffectStacking.Renew);
        var effect2 = new EffectData(ItemEffect.Luck, 5, true, EffectStacking.Renew);
        bonuses.ApplyEffect(effect1);
        bonuses.ApplyEffect(effect2);
        Assert.That(bonuses[ItemEffect.Luck].Percentage, Is.EqualTo(5));
    }

    [Test]
    public void ApplyEffect_SkipsNonPassive()
    {
        var bonuses = new Dictionary<ItemEffect, EffectValue>();
        var effect = new EffectData(ItemEffect.Luck, 10, false, EffectStacking.Stack);
        bonuses.ApplyEffect(effect);
        Assert.That(bonuses.ContainsKey(ItemEffect.Luck), Is.False);
    }

    [Test]
    public void ApplyEffect_StacksFlatAndPercentComponents()
    {
        var bonuses = new Dictionary<ItemEffect, EffectValue>();
        var percentOnly = new EffectData(ItemEffect.DamageReflect, 10, true, EffectStacking.Stack, flatAmount: 0, isFlat: false);
        var flatOnly = new EffectData(ItemEffect.DamageReflect, 0, true, EffectStacking.Stack, flatAmount: 5, isFlat: true);

        bonuses.ApplyEffect(percentOnly);
        bonuses.ApplyEffect(flatOnly);

        Assert.That(bonuses[ItemEffect.DamageReflect].Percentage, Is.EqualTo(10));
        Assert.That(bonuses[ItemEffect.DamageReflect].Flat, Is.EqualTo(5));
    }
}
