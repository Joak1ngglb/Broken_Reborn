using System;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Spells;
using Intersect.GameObjects;
using NUnit.Framework;

namespace Intersect.Tests.GameObjects;

public class StatArrayCompatibilityTests
{
    private const string LegacySixStatJson = "[10,20,30,40,50,60]";

    [Test]
    public void ItemDescriptorStatsJson_BackfillsWillpowerWithZero()
    {
        var descriptor = new ItemDescriptor(Guid.NewGuid());
        descriptor.StatsJson = LegacySixStatJson;

        AssertLegacyStatsWerePreserved(descriptor.StatsGiven);
    }

    [Test]
    public void ClassDescriptorBaseStatsJson_BackfillsWillpowerWithZero()
    {
        var descriptor = new ClassDescriptor(Guid.NewGuid());
        descriptor.JsonBaseStats = LegacySixStatJson;

        AssertLegacyStatsWerePreserved(descriptor.BaseStat);
    }

    [Test]
    public void NpcDescriptorStatsJson_BackfillsWillpowerWithZero()
    {
        var descriptor = new NPCDescriptor(Guid.NewGuid());
        descriptor.JsonStat = LegacySixStatJson;

        AssertLegacyStatsWerePreserved(descriptor.Stats);
    }

    [Test]
    public void SetDescriptorStatsJson_BackfillsWillpowerWithZero()
    {
        var descriptor = new SetDescriptor(Guid.NewGuid());
        descriptor.StatsJson = LegacySixStatJson;

        AssertLegacyStatsWerePreserved(descriptor.Stats);
    }

    [Test]
    public void SpellCombatDescriptorStatDiffJson_BackfillsWillpowerWithZero()
    {
        var descriptor = new SpellCombatDescriptor();
        descriptor.StatDiffJson = LegacySixStatJson;

        AssertLegacyStatsWerePreserved(descriptor.StatDiff);
    }

    private static void AssertLegacyStatsWerePreserved(int[] stats)
    {
        Assert.That(stats.Length, Is.EqualTo(Enum.GetValues<Stat>().Length));
        Assert.That(stats[(int)Stat.Attack], Is.EqualTo(10));
        Assert.That(stats[(int)Stat.Intelligence], Is.EqualTo(20));
        Assert.That(stats[(int)Stat.Defense], Is.EqualTo(30));
        Assert.That(stats[(int)Stat.Vitality], Is.EqualTo(40));
        Assert.That(stats[(int)Stat.Speed], Is.EqualTo(50));
        Assert.That(stats[(int)Stat.Agility], Is.EqualTo(60));
        Assert.That(stats[(int)Stat.Willpower], Is.EqualTo(0));
    }
}
