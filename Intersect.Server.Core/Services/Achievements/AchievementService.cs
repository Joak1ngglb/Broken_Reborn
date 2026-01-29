using System;
using System.Collections.Generic;
using System.Linq;
using Intersect;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Quests;
using ServerAchievementProgress = Intersect.Server.Database.PlayerData.Players.AchievementProgress;
using Intersect.Server.Database.PlayerData.Shops;
using Intersect.Server.Entities;
using Intersect.Server.Entities.Events;
using Intersect.Server.Localization;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Intersect.GameObjects;

namespace Intersect.Server.Services.Achievements;

public static class AchievementService
{
    private enum AchievementTrigger
    {
        LevelUp,
        NpcKill,
        QuestCompleted,
        MapEntered,
        ItemCollected,
        ItemCrafted,
        DungeonCompleted,
    }

    public static void HandleLevelUp(Player player)
    {
        UpdateAchievements(
            player,
            AchievementTrigger.LevelUp,
            new AchievementContext(player.Level)
        );
    }

    public static void HandleNpcKill(Player player, Npc npc)
    {
        if (npc?.Descriptor == null)
        {
            return;
        }

        UpdateAchievements(
            player,
            AchievementTrigger.NpcKill,
            new AchievementContext(1, npc.Descriptor.Id)
        );
    }

    public static void HandleQuestCompleted(Player player, QuestDescriptor quest)
    {
        if (quest == null)
        {
            return;
        }

        UpdateAchievements(
            player,
            AchievementTrigger.QuestCompleted,
            new AchievementContext(1, quest.Id)
        );
    }

    public static void HandleMapEntered(Player player, MapController mapController)
    {
        if (mapController == null)
        {
            return;
        }

        UpdateAchievements(
            player,
            AchievementTrigger.MapEntered,
            new AchievementContext(1, mapController.Id, mapController.ZoneType)
        );
    }

    public static void HandleItemCollected(Player player, ItemDescriptor itemDescriptor, int quantity)
    {
        if (itemDescriptor == null || quantity <= 0)
        {
            return;
        }

        UpdateAchievements(
            player,
            AchievementTrigger.ItemCollected,
            new AchievementContext(quantity, itemDescriptor.Id)
        );
    }

    public static void HandleItemCrafted(Player player, CraftingRecipeDescriptor recipe, ItemDescriptor itemDescriptor, int quantity)
    {
        if (recipe == null || itemDescriptor == null || quantity <= 0)
        {
            return;
        }

        UpdateAchievements(
            player,
            AchievementTrigger.ItemCrafted,
            new AchievementContext(quantity, itemDescriptor.Id)
        );
    }

    public static void HandleDungeonCompleted(Player player, Guid mapId)
    {
        if (mapId == Guid.Empty)
        {
            return;
        }

        UpdateAchievements(
            player,
            AchievementTrigger.DungeonCompleted,
            new AchievementContext(1, mapId)
        );
    }

    private static void UpdateAchievements(Player player, AchievementTrigger trigger, AchievementContext context)
    {
        if (player == null)
        {
            return;
        }

        var hasChanges = false;
        var achievementDescriptors = AchievementDescriptor.Lookup.Values
            .OfType<AchievementDescriptor>()
            .ToList();

        foreach (var achievement in achievementDescriptors)
        {
            if (!IsTriggerRelevant(achievement, trigger, context))
            {
                continue;
            }

            var progress = GetOrCreateProgress(player, achievement, ref hasChanges);
            if (progress.Completed)
            {
                continue;
            }

            var hasConditionProgress = UpdateProgressFromConditions(player, achievement, progress);
            if (!hasConditionProgress)
            {
                var updatedProgress = progress.Progress + Math.Max(context.ProgressDelta, 0);
                if (updatedProgress != progress.Progress)
                {
                    progress.Progress = updatedProgress;
                    hasChanges = true;
                }
            }

            if (IsAchievementCompleted(player, achievement, progress))
            {
                CompleteAchievement(player, achievement, progress);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            player.Save();
            PacketSender.SendAchievementProgress(player);
        }
    }

    private static bool IsTriggerRelevant(
        AchievementDescriptor achievement,
        AchievementTrigger trigger,
        AchievementContext context
    )
    {
        if (achievement.MetaAchievementIds.Count > 0)
        {
            return true;
        }

        return trigger switch
        {
            AchievementTrigger.LevelUp =>
                achievement.Category == AchievementCategory.Eventos ||
                HasLevelRequirement(achievement.Requirements),
            AchievementTrigger.NpcKill => achievement.Category == AchievementCategory.Monstruos,
            AchievementTrigger.QuestCompleted => MatchesQuestTrigger(achievement, context.TargetId),
            AchievementTrigger.MapEntered => MatchesMapTrigger(achievement, context),
            AchievementTrigger.ItemCollected or AchievementTrigger.ItemCrafted =>
                MatchesItemTrigger(achievement, context.TargetId),
            AchievementTrigger.DungeonCompleted => achievement.Category == AchievementCategory.Mazmorras,
            _ => false
        };
    }

    private static ServerAchievementProgress GetOrCreateProgress(
        Player player,
        AchievementDescriptor achievement,
        ref bool hasChanges
    )
    {
        var progress = player.Achievements.FirstOrDefault(p => p.AchievementId == achievement.Id);
        if (progress != null)
        {
            return progress;
        }

        progress = new ServerAchievementProgress(achievement.Id);
        player.Achievements.Add(progress);
        hasChanges = true;
        return progress;
    }

    private static bool UpdateProgressFromConditions(
        Player player,
        AchievementDescriptor achievement,
        ServerAchievementProgress progress
    )
    {
        var progressValues = new List<int>();

        foreach (var condition in achievement.Requirements.Lists.SelectMany(list => list.Conditions))
        {
            switch (condition)
            {
                case LevelOrStatCondition levelCondition when levelCondition.ComparingLevel:
                    progressValues.Add(player.Level);
                    break;
                case HasItemCondition hasItemCondition:
                    progressValues.Add(
                        player.CountItems(
                            hasItemCondition.ItemId,
                            true,
                            hasItemCondition.CheckBank
                        )
                    );
                    break;
                case QuestCompletedCondition questCompletedCondition:
                    progressValues.Add(player.QuestCompleted(questCompletedCondition.QuestId) ? 1 : 0);
                    break;
                case QuestInProgressCondition questInProgressCondition:
                    progressValues.Add(
                        player.QuestInProgress(
                            questInProgressCondition.QuestId,
                            questInProgressCondition.Progress,
                            questInProgressCondition.TaskId
                        )
                            ? 1
                            : 0
                    );
                    break;
                case MapIsCondition mapCondition:
                    progressValues.Add(player.MapId == mapCondition.MapId ? 1 : 0);
                    break;
                case MapZoneTypeIs zoneCondition:
                    progressValues.Add(player.Map?.ZoneType == zoneCondition.ZoneType ? 1 : 0);
                    break;
                case VariableIsCondition variableCondition when variableCondition.VariableType == VariableType.PlayerVariable:
                    progressValues.Add((int)player.GetVariableValue(variableCondition.VariableId).Integer);
                    break;
            }
        }

        if (progressValues.Count == 0)
        {
            return false;
        }

        var updatedProgress = progressValues.Max();
        if (updatedProgress != progress.Progress)
        {
            progress.Progress = updatedProgress;
        }

        return true;
    }

    private static bool IsAchievementCompleted(
        Player player,
        AchievementDescriptor achievement,
        ServerAchievementProgress progress
    )
    {
        if (achievement.MetaAchievementIds.Count > 0)
        {
            return achievement.MetaAchievementIds.All(
                achievementId => player.Achievements.Any(p => p.AchievementId == achievementId && p.Completed)
            );
        }

        if (!Conditions.MeetsConditionLists(achievement.Requirements, player, null))
        {
            return false;
        }

        if (achievement.Requirements.Lists.Count == 0)
        {
            return progress.Progress > 0;
        }

        return true;
    }

    private static void CompleteAchievement(
        Player player,
        AchievementDescriptor achievement,
        ServerAchievementProgress progress
    )
    {
        progress.Completed = true;
        progress.CompletedAt = DateTime.UtcNow;

        GrantRewards(player, achievement);
        PacketSender.SendAchievementCompleted(player, achievement);

        PacketSender.SendChatMsg(
            player,
            Strings.Achievements.CompletedNotification.ToString(achievement.Name),
            ChatMessageType.Notice,
            CustomColors.Alerts.Success
        );
    }

    private static void GrantRewards(Player player, AchievementDescriptor achievement)
    {
        var rewards = achievement.Rewards;
        if (rewards.Experience > 0)
        {
            player.GiveExperience(rewards.Experience);
        }

        if (rewards.Currency > 0)
        {
            var currency = PlayerShopManager.ResolveGlobalCurrencyDescriptor();
            if (currency != null)
            {
                GrantItemStacks(player, currency.Id, rewards.Currency);
            }
        }

        foreach (var reward in rewards.Resources)
        {
            if (reward.Value <= 0)
            {
                continue;
            }

            GrantItemStacks(player, reward.Key, reward.Value);
        }

        foreach (var titleId in rewards.TitleIds)
        {
            if (!player.UnlockedTitles.Contains(titleId))
            {
                player.UnlockedTitles.Add(titleId);
            }
        }

        foreach (var ornamentId in rewards.OrnamentIds)
        {
            if (!player.UnlockedOrnaments.Contains(ornamentId))
            {
                player.UnlockedOrnaments.Add(ornamentId);
            }
        }
    }

    private static void GrantItemStacks(Player player, Guid itemId, long quantity)
    {
        var remaining = quantity;
        while (remaining > 0)
        {
            var stackAmount = (int)Math.Min(remaining, int.MaxValue);
            player.TryGiveItem(itemId, stackAmount, ItemHandling.Overflow);
            remaining -= stackAmount;
        }
    }

    private static bool ContainsCondition<TCondition>(ConditionLists requirements) where TCondition : Condition
    {
        return requirements.Lists.Any(list => list.Conditions.OfType<TCondition>().Any());
    }

    private static bool HasLevelRequirement(ConditionLists requirements)
    {
        return requirements.Lists.Any(
            list => list.Conditions.OfType<LevelOrStatCondition>().Any(condition => condition.ComparingLevel)
        );
    }

    private static bool MatchesQuestTrigger(AchievementDescriptor achievement, Guid? questId)
    {
        var hasQuestCondition = ContainsCondition<QuestCompletedCondition>(achievement.Requirements);
        if (hasQuestCondition)
        {
            return MatchesQuestCondition(achievement.Requirements, questId);
        }

        return achievement.Category == AchievementCategory.Misiones;
    }

    private static bool MatchesMapTrigger(AchievementDescriptor achievement, AchievementContext context)
    {
        var hasMapConditions =
            ContainsCondition<MapIsCondition>(achievement.Requirements) ||
            ContainsCondition<MapZoneTypeIs>(achievement.Requirements);
        if (hasMapConditions)
        {
            return MatchesMapConditions(achievement.Requirements, context);
        }

        return achievement.Category == AchievementCategory.Exploracion;
    }

    private static bool MatchesItemTrigger(AchievementDescriptor achievement, Guid? itemId)
    {
        var hasItemCondition = ContainsCondition<HasItemCondition>(achievement.Requirements);
        if (hasItemCondition)
        {
            return MatchesItemCondition(achievement.Requirements, itemId);
        }

        return achievement.Category == AchievementCategory.Oficios;
    }

    private static bool MatchesQuestCondition(ConditionLists requirements, Guid? questId)
    {
        if (questId == null)
        {
            return false;
        }

        return requirements.Lists.Any(
            list => list.Conditions.OfType<QuestCompletedCondition>().Any(condition => condition.QuestId == questId)
        );
    }

    private static bool MatchesItemCondition(ConditionLists requirements, Guid? itemId)
    {
        if (itemId == null)
        {
            return false;
        }

        return requirements.Lists.Any(
            list => list.Conditions.OfType<HasItemCondition>().Any(condition => condition.ItemId == itemId)
        );
    }

    private static bool MatchesMapConditions(ConditionLists requirements, AchievementContext context)
    {
        var matchesMap = requirements.Lists.Any(
            list => list.Conditions.OfType<MapIsCondition>().Any(condition => condition.MapId == context.TargetId)
        );

        var matchesZone = requirements.Lists.Any(
            list => list.Conditions.OfType<MapZoneTypeIs>().Any(condition => condition.ZoneType == context.ZoneType)
        );

        return matchesMap || matchesZone;
    }

    private sealed class AchievementContext
    {
        public AchievementContext(int progressDelta, Guid? targetId = null, MapZone? zoneType = null)
        {
            ProgressDelta = progressDelta;
            TargetId = targetId;
            ZoneType = zoneType;
        }

        public int ProgressDelta { get; }

        public Guid? TargetId { get; }

        public MapZone? ZoneType { get; }
    }
}
