using System;
using System.IO;
using System.Linq;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;
using Intersect.Server.Database;
using Intersect.Server.Entities;
using Intersect.Server.Services.Achievements;
using NUnit.Framework;

namespace Intersect.Tests.Server.Services;

[TestFixture]
public class AchievementServiceTests
{
    [SetUp]
    public void SetUp()
    {
        Options.EnsureCreated();
        Directory.CreateDirectory("resources");
        AchievementDescriptor.Lookup.Clear();
    }

    [Test]
    public void CompleteAchievement_ReevaluatesDependentMetaAchievements()
    {
        var player = CreatePlayer();
        var baseAchievement = new AchievementDescriptor(Guid.NewGuid())
        {
            Name = "Base Achievement",
            Category = AchievementCategory.Events,
        };
        var metaAchievement = new AchievementDescriptor(Guid.NewGuid())
        {
            Name = "Meta Achievement",
            MetaAchievementIds = new System.Collections.Generic.List<Guid> { baseAchievement.Id },
            CompletionMode = AchievementCompletionMode.AndGlobal,
        };

        AchievementDescriptor.Lookup.Set(baseAchievement.Id, baseAchievement);
        AchievementDescriptor.Lookup.Set(metaAchievement.Id, metaAchievement);
        AchievementService.RebuildAchievementIndices();

        AchievementService.CompleteAchievement(player, baseAchievement.Id);

        var metaProgress = player.Achievements.FirstOrDefault(
            progress => progress.AchievementId == metaAchievement.Id
        );
        Assert.That(metaProgress, Is.Not.Null);
        Assert.That(metaProgress!.Completed, Is.True);
    }

    [Test]
    public void HandleLevelUp_UsesConditionProgress()
    {
        var player = CreatePlayer();
        player.Level = 5;
        var achievement = new AchievementDescriptor(Guid.NewGuid())
        {
            Name = "Level Condition",
            Requirements = new ConditionLists
            {
                Lists = new System.Collections.Generic.List<ConditionList>
                {
                    new ConditionList
                    {
                        Conditions = new System.Collections.Generic.List<Condition>
                        {
                            new LevelOrStatCondition
                            {
                                ComparingLevel = true,
                                Value = 5,
                            }
                        }
                    }
                }
            },
        };

        AchievementDescriptor.Lookup.Set(achievement.Id, achievement);
        AchievementService.RebuildAchievementIndices();

        AchievementService.HandleLevelUp(player);

        var progress = player.Achievements.FirstOrDefault(p => p.AchievementId == achievement.Id);
        Assert.That(progress, Is.Not.Null);
        Assert.That(progress!.Progress, Is.EqualTo(1));
        Assert.That(progress.Objectives.Count, Is.EqualTo(1));
        Assert.That(progress.Objectives[0].Current, Is.EqualTo(player.Level));
        Assert.That(progress.Objectives[0].Target, Is.EqualTo(5));
        Assert.That(progress.Completed, Is.True);
    }

    private static Player CreatePlayer()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Name = "Achievement Test Player",
        };

        using (var context = DbInterface.CreatePlayerContext(readOnly: false))
        {
            context.Database.EnsureCreated();
            context.Players.Add(player);
            context.SaveChanges();
        }

        return player;
    }
}
