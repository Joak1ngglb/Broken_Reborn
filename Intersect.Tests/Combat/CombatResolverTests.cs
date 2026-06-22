using System.IO;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Server.Entities;
using Intersect.Server.Entities.Combat;
using Intersect.Server.General;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Intersect.Tests.Combat;

public class CombatResolverTests
{
    private static string FormulasDirectory => Path.Combine(TestContext.CurrentContext.WorkDirectory, "resources");
    private static string FormulasFile => Path.Combine(FormulasDirectory, "formulas.json");

    [SetUp]
    public void Setup()
    {
        Options.EnsureCreated();
    }

    [Test]
    public void BuildDefenseOverrides_OnlyAppliesArmorPenetrationToPhysicalDamage()
    {
        var attacker = new Player();
        var defender = new Player();
        defender.BaseStats[(int)Stat.Defense] = 200;

        var attackerEffects = new CombatantEffects(
            attacker,
            CombatEffectCollection.From([new EffectData(ItemEffect.ArmorPenetration, 25, isPassive: false)])
        );

        var physicalOverrides = CombatResolver.BuildDefenseOverrides(
            DamageType.Physical,
            defender,
            attackerEffects
        );
        var magicOverrides = CombatResolver.BuildDefenseOverrides(
            DamageType.Magic,
            defender,
            attackerEffects
        );

        Assert.That(physicalOverrides, Is.Not.Null);
        Assert.That(physicalOverrides, Contains.Key("V_Defense"));
        Assert.That(Convert.ToSingle(physicalOverrides!["V_Defense"]), Is.EqualTo(150f).Within(0.001f));
        Assert.That(magicOverrides, Is.Null);
    }

    [Test]
    public void CalculateDamage_UsesWillpowerForMagicResistance()
    {
        WriteFormulasFile(
            magicDamage: "V_MagicResist",
            physicalDamage: "V_Defense",
            trueDamage: "0"
        );
        Formulas.LoadFormulas();

        var attacker = new Player();
        var defender = new Player();
        defender.BaseStats[(int)Stat.Vitality] = 10;
        defender.BaseStats[(int)Stat.Willpower] = 42;

        var result = Formulas.CalculateDamage(
            1,
            DamageType.Magic,
            Stat.Attack,
            0,
            1d,
            attacker,
            defender
        );

        Assert.That(result, Is.EqualTo(42));
    }

    private static void WriteFormulasFile(string magicDamage, string physicalDamage, string trueDamage)
    {
        Directory.CreateDirectory(FormulasDirectory);

        var formulas = new
        {
            MagicDamage = magicDamage,
            PhysicalDamage = physicalDamage,
            TrueDamage = trueDamage,
        };

        File.WriteAllText(FormulasFile, JsonConvert.SerializeObject(formulas, Formatting.Indented));
    }
}
