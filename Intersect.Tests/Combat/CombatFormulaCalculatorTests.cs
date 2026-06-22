using Intersect.Framework.Core.Combat;
using NUnit.Framework;

namespace Intersect.Tests.Combat;

public class CombatFormulaCalculatorTests
{
    [Test]
    public void CalculateHitChance_UsesSeventyFivePercentParityByDefault()
    {
        var hitChance = CombatFormulaCalculator.CalculateHitChance(100d, 100d);

        Assert.That(hitChance, Is.EqualTo(0.75d).Within(0.0001d));
    }

    [Test]
    public void ApplyCriticalReduction_ReducesCriticalMultiplierSubtractively()
    {
        var reducedMultiplier = CombatFormulaCalculator.ApplyCriticalReduction(2.0d, 30);

        Assert.That(reducedMultiplier, Is.EqualTo(1.7d).Within(0.0001d));
    }

    [Test]
    public void ApplyCriticalReduction_DoesNotAffectNormalHits()
    {
        var reducedMultiplier = CombatFormulaCalculator.ApplyCriticalReduction(1.0d, 30);

        Assert.That(reducedMultiplier, Is.EqualTo(1.0d).Within(0.0001d));
    }

    [Test]
    public void ApplyCriticalReduction_NeverDropsBelowNormalDamage()
    {
        var reducedMultiplier = CombatFormulaCalculator.ApplyCriticalReduction(1.2d, 50);

        Assert.That(reducedMultiplier, Is.EqualTo(1.0d).Within(0.0001d));
    }
}
