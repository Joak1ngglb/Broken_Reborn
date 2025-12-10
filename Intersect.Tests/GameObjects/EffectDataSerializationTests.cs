using Intersect.Framework.Core.GameObjects.Items;
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
}
