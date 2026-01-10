using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Intersect.Tests.GameObjects;

public class EffectDataSerializationTests
{
    [TestCase(ItemEffect.AntiCritChance)]
    [TestCase(ItemEffect.ArmorPenetration)]
    [TestCase(ItemEffect.DamageReduction)]
    [TestCase(ItemEffect.DamageReflect)]
    public void EffectDataSerializesNewItemEffects(ItemEffect itemEffect)
    {
        var effect = new EffectData(itemEffect, 25, isPassive: false, EffectStacking.Renew, flatAmount: 10, isFlat: true);

        var json = JsonConvert.SerializeObject(effect);
        var roundTrip = JsonConvert.DeserializeObject<EffectData>(json);

        Assert.That(roundTrip, Is.Not.Null);
        Assert.That(roundTrip!.Type, Is.EqualTo(itemEffect));
        Assert.That(roundTrip.Percentage, Is.EqualTo(effect.Percentage));
        Assert.That(roundTrip.FlatAmount, Is.EqualTo(effect.FlatAmount));
        Assert.That(roundTrip.IsFlat, Is.EqualTo(effect.IsFlat));
        Assert.That(roundTrip.IsPassive, Is.EqualTo(effect.IsPassive));
        Assert.That(roundTrip.Stacking, Is.EqualTo(effect.Stacking));
    }

    [TestCase(ItemEffect.AntiCritChance)]
    [TestCase(ItemEffect.ArmorPenetration)]
    [TestCase(ItemEffect.DamageReduction)]
    [TestCase(ItemEffect.DamageReflect)]
    public void ItemDescriptorNormalizesFlatEffectDataOnLoad(ItemEffect itemEffect)
    {
        var effect = new EffectData(itemEffect, 25, isPassive: false, EffectStacking.Renew, flatAmount: 10, isFlat: true);
        var effectsJson = JsonConvert.SerializeObject(new[] { effect });

        var item = new ItemDescriptor();
        item.EffectsJson = effectsJson;

        Assert.That(item.Effects, Has.Count.EqualTo(1));
        Assert.That(item.Effects[0].Type, Is.EqualTo(itemEffect));
        Assert.That(item.Effects[0].Percentage, Is.EqualTo(effect.Percentage));
        Assert.That(item.Effects[0].FlatAmount, Is.EqualTo(0));
        Assert.That(item.Effects[0].IsFlat, Is.False);
    }

    [TestCase(ItemEffect.AntiCritChance)]
    [TestCase(ItemEffect.ArmorPenetration)]
    [TestCase(ItemEffect.DamageReduction)]
    [TestCase(ItemEffect.DamageReflect)]
    public void SetDescriptorNormalizesFlatEffectDataOnLoad(ItemEffect itemEffect)
    {
        var effect = new EffectData(itemEffect, 25, isPassive: false, EffectStacking.Renew, flatAmount: 10, isFlat: true);
        var effectsJson = JsonConvert.SerializeObject(new[] { effect });

        var setDescriptor = new SetDescriptor();
        setDescriptor.EffectsJson = effectsJson;

        Assert.That(setDescriptor.Effects, Has.Count.EqualTo(1));
        Assert.That(setDescriptor.Effects[0].Type, Is.EqualTo(itemEffect));
        Assert.That(setDescriptor.Effects[0].Percentage, Is.EqualTo(effect.Percentage));
        Assert.That(setDescriptor.Effects[0].FlatAmount, Is.EqualTo(0));
        Assert.That(setDescriptor.Effects[0].IsFlat, Is.False);
    }
}
