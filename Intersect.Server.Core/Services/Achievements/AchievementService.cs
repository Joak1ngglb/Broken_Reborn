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
using Intersect.Framework.Core.GameObjects.Titles;
using ServerAchievementProgress = Intersect.Server.Database.PlayerData.Players.AchievementProgress;
using Intersect.Server.Database.PlayerData.Shops;
using Intersect.Server.Entities;
using Intersect.Server.Entities.Events;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Localization;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Intersect.GameObjects;

namespace Intersect.Server.Services.Achievements;

public static class AchievementService
{
    private static readonly object IndexLock = new();
    private static AchievementIndex _achievementIndex = AchievementIndex.Empty;

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

    public static void RebuildAchievementIndices()
    {
        lock (IndexLock)
        {
            _achievementIndex = AchievementIndex.Build();
        }
    }

    public static void CompleteAchievement(Player player, Guid achievementId)
    {
        if (player == null || achievementId == Guid.Empty)
        {
            return;
        }

        var achievement = AchievementDescriptor.Get(achievementId);
        if (achievement == null)
        {
            return;
        }

        var hasChanges = false;
        var progress = GetOrCreateProgress(player, achievement, ref hasChanges);
        if (progress.Completed)
        {
            return;
        }

        var objectives = GetAchievementObjectives(achievement, player);
        if (objectives.Count > 0)
        {
            for (var i = 0; i < objectives.Count; i++)
            {
                var objective = objectives[i];
                objective.Current = objective.Target > 0 ? objective.Target : 1;
                objectives[i] = objective;
            }

            progress.Objectives = objectives;
            progress.Progress = objectives.Count;
        }
        else
        {
            progress.Progress = Math.Max(progress.Progress, 1);
        }

        CompleteAchievement(player, achievement, progress);
        hasChanges = true;
        var progressById = BuildAchievementProgressIndex(player);
        ProcessDependentMetaAchievements(player, achievement, progressById, ref hasChanges);

        if (hasChanges)
        {
            player.Save();
            PacketSender.SendAchievementProgress(player);
        }
    }

    public static void CompleteAchievementObjective(Player player, Guid achievementId, int objectiveIndex)
    {
        if (player == null || achievementId == Guid.Empty || objectiveIndex < 0)
        {
            return;
        }

        var achievement = AchievementDescriptor.Get(achievementId);
        if (achievement == null)
        {
            return;
        }

        var hasChanges = false;
        var progress = GetOrCreateProgress(player, achievement, ref hasChanges);
        if (progress.Completed)
        {
            return;
        }

        var objectives = progress.Objectives.Count > 0
            ? progress.Objectives
            : GetAchievementObjectives(achievement, player);

        if (objectiveIndex >= objectives.Count)
        {
            return;
        }

        var target = objectives[objectiveIndex].Target > 0 ? objectives[objectiveIndex].Target : 1;
        if (objectives[objectiveIndex].Current != target)
        {
            objectives[objectiveIndex].Current = target;
            progress.Objectives = objectives;
            hasChanges = true;
        }

        var updatedProgress = objectives.Count(objective => objective.IsCompleted);
        if (updatedProgress != progress.Progress)
        {
            progress.Progress = updatedProgress;
            hasChanges = true;
        }

        if (updatedProgress == objectives.Count && objectives.Count > 0)
        {
            CompleteAchievement(player, achievement, progress);
            hasChanges = true;
            var progressById = BuildAchievementProgressIndex(player);
            ProcessDependentMetaAchievements(player, achievement, progressById, ref hasChanges);
        }

        if (hasChanges)
        {
            player.Save();
            PacketSender.SendAchievementProgress(player);
        }
    }

    private static void UpdateAchievements(Player player, AchievementTrigger trigger, AchievementContext context)
    {
        if (player == null)
        {
            return;
        }

        var hasChanges = false;
        var progressById = BuildAchievementProgressIndex(player);
        var achievementDescriptors = GetIndexedAchievements(trigger, context);

        foreach (var achievement in achievementDescriptors)
        {
            if (!IsTriggerRelevant(achievement, trigger, context))
            {
                continue;
            }

            var progress = GetOrCreateProgress(player, achievement, progressById, ref hasChanges);
            if (progress.Completed)
            {
                continue;
            }

            var hasConditionProgress = UpdateProgressFromConditions(player, achievement, progress, ref hasChanges);
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
                ProcessDependentMetaAchievements(player, achievement, progressById, ref hasChanges);
            }
        }

        if (hasChanges)
        {
            player.Save();
            PacketSender.SendAchievementProgress(player);
        }
    }

    private static IReadOnlyList<AchievementDescriptor> GetIndexedAchievements(
        AchievementTrigger trigger,
        AchievementContext context
    )
    {
        var index = _achievementIndex;
        var achievements = new HashSet<AchievementDescriptor>();

        void AddRange(IEnumerable<AchievementDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                achievements.Add(descriptor);
            }
        }

        switch (trigger)
        {
            case AchievementTrigger.LevelUp:
                AddRange(index.LevelUp);
                break;
            case AchievementTrigger.NpcKill:
                AddRange(index.NpcKill);
                break;
            case AchievementTrigger.QuestCompleted:
                if (context.TargetId is { } questId &&
                    index.QuestById.TryGetValue(questId, out var questAchievements))
                {
                    AddRange(questAchievements);
                }

                AddRange(index.QuestGeneral);
                break;
            case AchievementTrigger.MapEntered:
                if (context.TargetId is { } mapId &&
                    index.MapById.TryGetValue(mapId, out var mapAchievements))
                {
                    AddRange(mapAchievements);
                }

                if (context.ZoneType is { } zoneType &&
                    index.MapByZoneType.TryGetValue(zoneType, out var zoneAchievements))
                {
                    AddRange(zoneAchievements);
                }

                AddRange(index.MapGeneral);
                break;
            case AchievementTrigger.ItemCollected:
            case AchievementTrigger.ItemCrafted:
                if (context.TargetId is { } itemId &&
                    index.ItemById.TryGetValue(itemId, out var itemAchievements))
                {
                    AddRange(itemAchievements);
                }

                AddRange(index.ItemGeneral);
                break;
            case AchievementTrigger.DungeonCompleted:
                AddRange(index.DungeonCompleted);
                break;
        }

        return achievements.ToList();
    }

    private static bool IsTriggerRelevant(
        AchievementDescriptor achievement,
        AchievementTrigger trigger,
        AchievementContext context
    )
    {
        if (achievement.MetaAchievementIds.Count > 0)
        {
            return false;
        }

        return trigger switch
        {
            AchievementTrigger.LevelUp =>
                achievement.Category == AchievementCategory.Events ||
                HasLevelRequirement(achievement.Requirements),
            AchievementTrigger.NpcKill => achievement.Category == AchievementCategory.Monsters,
            AchievementTrigger.QuestCompleted => MatchesQuestTrigger(achievement, context.TargetId),
            AchievementTrigger.MapEntered => MatchesMapTrigger(achievement, context),
            AchievementTrigger.ItemCollected or AchievementTrigger.ItemCrafted =>
                MatchesItemTrigger(achievement, context.TargetId),
            AchievementTrigger.DungeonCompleted => achievement.Category == AchievementCategory.Dungeons,
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

    private static ServerAchievementProgress GetOrCreateProgress(
        Player player,
        AchievementDescriptor achievement,
        Dictionary<Guid, ServerAchievementProgress> progressById,
        ref bool hasChanges
    )
    {
        if (progressById.TryGetValue(achievement.Id, out var progress))
        {
            return progress;
        }

        progress = new ServerAchievementProgress(achievement.Id);
        player.Achievements.Add(progress);
        progressById[achievement.Id] = progress;
        hasChanges = true;
        return progress;
    }

    private static Dictionary<Guid, ServerAchievementProgress> BuildAchievementProgressIndex(Player player)
    {
        var progressById = new Dictionary<Guid, ServerAchievementProgress>(player.Achievements.Count);
        foreach (var progress in player.Achievements)
        {
            progressById.TryAdd(progress.AchievementId, progress);
        }

        return progressById;
    }

    private static bool UpdateProgressFromConditions(
        Player player,
        AchievementDescriptor achievement,
        ServerAchievementProgress progress,
        ref bool hasChanges
    )
    {
        var objectives = GetAchievementObjectives(achievement, player);
        if (objectives.Count == 0)
        {
            if (progress.Objectives.Count > 0)
            {
                progress.Objectives = [];
                hasChanges = true;
            }

            return false;
        }

        if (!AreObjectivesEqual(progress.Objectives, objectives))
        {
            progress.Objectives = objectives;
            hasChanges = true;
        }

        var updatedProgress = objectives.Count(objective => objective.Current > 0);
        if (updatedProgress != progress.Progress)
        {
            progress.Progress = updatedProgress;
            hasChanges = true;
        }

        return true;
    }

    public static List<ObjectiveProgress> GetAchievementObjectives(
        AchievementDescriptor achievement,
        Player player
    )
    {
        var objectives = new List<ObjectiveProgress>();
        if (achievement == null || player == null)
        {
            return objectives;
        }

        if (achievement.MetaAchievementIds.Count > 0)
        {
            foreach (var metaAchievementId in achievement.MetaAchievementIds)
            {
                var isCompleted = player.Achievements.Any(
                    progress => progress.AchievementId == metaAchievementId && progress.Completed
                );
                objectives.Add(new ObjectiveProgress(isCompleted ? 1 : 0, 1, ProgressMode.Binary));
            }

            return objectives;
        }

        foreach (var condition in achievement.Requirements.Lists.SelectMany(list => list.Conditions))
        {
            switch (condition)
            {
                case LevelOrStatCondition levelCondition:
                {
                    var currentValue = levelCondition.ComparingLevel
                        ? player.Level
                        : player.GetStatValue(levelCondition.Stat);
                    objectives.Add(new ObjectiveProgress(currentValue, levelCondition.Value, ProgressMode.Quantitative));
                    break;
                }
                case HasItemCondition hasItemCondition:
                {
                    var currentValue = player.CountItems(
                        hasItemCondition.ItemId,
                        true,
                        hasItemCondition.CheckBank
                    );
                    objectives.Add(new ObjectiveProgress(currentValue, hasItemCondition.Quantity, ProgressMode.Quantitative));
                    break;
                }
                case QuestCompletedCondition questCompletedCondition:
                {
                    var currentValue = player.QuestCompleted(questCompletedCondition.QuestId) ? 1 : 0;
                    objectives.Add(new ObjectiveProgress(currentValue, 1, ProgressMode.Binary));
                    break;
                }
                case QuestInProgressCondition questInProgressCondition:
                {
                    var currentValue = player.QuestInProgress(
                        questInProgressCondition.QuestId,
                        questInProgressCondition.Progress,
                        questInProgressCondition.TaskId
                    )
                        ? 1
                        : 0;
                    objectives.Add(new ObjectiveProgress(currentValue, 1, ProgressMode.Binary));
                    break;
                }
                case MapIsCondition mapCondition:
                {
                    var currentValue = player.MapId == mapCondition.MapId ? 1 : 0;
                    objectives.Add(new ObjectiveProgress(currentValue, 1, ProgressMode.Binary));
                    break;
                }
                case MapZoneTypeIs zoneCondition:
                {
                    var currentValue = player.Map?.ZoneType == zoneCondition.ZoneType ? 1 : 0;
                    objectives.Add(new ObjectiveProgress(currentValue, 1, ProgressMode.Binary));
                    break;
                }
                case VariableIsCondition variableCondition when variableCondition.VariableType == VariableType.PlayerVariable:
                {
                    var currentValue = (int)player.GetVariableValue(variableCondition.VariableId).Integer;
                    if (variableCondition.Comparison is IntegerVariableComparison intComparison)
                    {
                        var targetValue = intComparison.MaxValue > 0
                            ? (int)Math.Min(intComparison.MaxValue, int.MaxValue)
                            : (int)Math.Min(intComparison.Value, int.MaxValue);

                        if (targetValue > 0)
                        {
                            objectives.Add(new ObjectiveProgress(currentValue, targetValue, ProgressMode.Quantitative));
                            break;
                        }
                    }

                    var meetsCondition = Conditions.MeetsCondition(variableCondition, player, null, null) ? 1 : 0;
                    objectives.Add(new ObjectiveProgress(meetsCondition, 1, ProgressMode.Binary));
                    break;
                }
                default:
                {
                    var meetsCondition = Conditions.MeetsCondition(condition, player, null, null) ? 1 : 0;
                    objectives.Add(new ObjectiveProgress(meetsCondition, 1, ProgressMode.Binary));
                    break;
                }
            }
        }

        return objectives;
    }

    private static bool AreObjectivesEqual(
        IReadOnlyList<ObjectiveProgress> left,
        IReadOnlyList<ObjectiveProgress> right
    )
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            var leftObjective = left[index];
            var rightObjective = right[index];
            if (leftObjective.Current != rightObjective.Current ||
                leftObjective.Target != rightObjective.Target ||
                leftObjective.Mode != rightObjective.Mode)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAchievementCompleted(
        Player player,
        AchievementDescriptor achievement,
        ServerAchievementProgress progress
    )
    {
        return achievement.MetaAchievementIds.Count > 0
            ? AreMetaAchievementsComplete(player, achievement)
            : AreRequirementsComplete(player, achievement, progress);
    }

    private static bool AreMetaAchievementsComplete(Player player, AchievementDescriptor achievement)
    {
        var anyCompleted = achievement.MetaAchievementIds.Any(
            achievementId => player.Achievements.Any(p => p.AchievementId == achievementId && p.Completed)
        );
        var allCompleted = achievement.MetaAchievementIds.All(
            achievementId => player.Achievements.Any(p => p.AchievementId == achievementId && p.Completed)
        );

        return achievement.CompletionMode switch
        {
            AchievementCompletionMode.OrGlobal => anyCompleted,
            AchievementCompletionMode.AndGlobal or AchievementCompletionMode.OrListsAndConditions => allCompleted,
            _ => allCompleted
        };
    }

    private static bool AreRequirementsComplete(
        Player player,
        AchievementDescriptor achievement,
        ServerAchievementProgress progress
    )
    {
        if (achievement.Requirements.Lists.Count == 0)
        {
            return progress.Progress > 0;
        }

        return achievement.CompletionMode switch
        {
            AchievementCompletionMode.AndGlobal => Conditions.MeetsConditionLists(
                achievement.Requirements,
                player,
                null,
                false
            ),
            AchievementCompletionMode.OrGlobal => MeetsAnyCondition(achievement.Requirements, player),
            AchievementCompletionMode.OrListsAndConditions => Conditions.MeetsConditionLists(
                achievement.Requirements,
                player,
                null
            ),
            _ => Conditions.MeetsConditionLists(achievement.Requirements, player, null)
        };
    }

    private static bool MeetsAnyCondition(ConditionLists requirements, Player player)
    {
        return requirements.Lists
            .SelectMany(list => list.Conditions)
            .Any(condition => Conditions.MeetsCondition(condition, player, null, null));
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

    private static void ProcessDependentMetaAchievements(
        Player player,
        AchievementDescriptor completedAchievement,
        Dictionary<Guid, ServerAchievementProgress> progressById,
        ref bool hasChanges
    )
    {
        var index = _achievementIndex;
        if (!index.MetaByAchievementId.TryGetValue(completedAchievement.Id, out var metaAchievements) ||
            metaAchievements.Count == 0)
        {
            return;
        }

        var pending = new Queue<AchievementDescriptor>(metaAchievements);
        var processed = new HashSet<Guid>();
        while (pending.Count > 0)
        {
            var metaAchievement = pending.Dequeue();
            if (metaAchievement == null || !processed.Add(metaAchievement.Id))
            {
                continue;
            }

            var progress = GetOrCreateProgress(player, metaAchievement, progressById, ref hasChanges);
            if (progress.Completed)
            {
                continue;
            }

            UpdateProgressFromConditions(player, metaAchievement, progress, ref hasChanges);

            if (!IsAchievementCompleted(player, metaAchievement, progress))
            {
                continue;
            }

            CompleteAchievement(player, metaAchievement, progress);
            hasChanges = true;

            if (index.MetaByAchievementId.TryGetValue(metaAchievement.Id, out var chainedMetaAchievements))
            {
                foreach (var chainedMetaAchievement in chainedMetaAchievements)
                {
                    pending.Enqueue(chainedMetaAchievement);
                }
            }
        }
    }

    private static void GrantRewards(Player player, AchievementDescriptor achievement)
    {
        var rewards = achievement.Rewards;
        var pendingAttachments = new List<MailAttachment>();
        if (rewards.Experience > 0)
        {
            player.GiveExperience(rewards.Experience);
        }

        if (rewards.Currency > 0)
        {
            var currency = PlayerShopManager.ResolveGlobalCurrencyDescriptor();
            if (currency != null)
            {
                GrantRewardItems(player, currency.Id, rewards.Currency, pendingAttachments);
            }
        }

        foreach (var reward in rewards.Items)
        {
            if (reward.Value <= 0)
            {
                continue;
            }

            GrantRewardItems(player, reward.Key, reward.Value, pendingAttachments);
        }

        foreach (var titleId in rewards.TitleIds)
        {
            if (!TitleDescriptor.Lookup.TryGetValue(titleId, out _))
            {
                continue;
            }

            if (!player.UnlockedTitles.Contains(titleId))
            {
                player.UnlockedTitles.Add(titleId);
            }
        }

        if (pendingAttachments.Count > 0)
        {
            SendRewardMail(player, achievement, pendingAttachments);
        }
    }

    private static void GrantRewardItems(
        Player player,
        Guid itemId,
        long quantity,
        ICollection<MailAttachment> pendingAttachments
    )
    {
        if (quantity <= 0 || itemId == Guid.Empty || ItemDescriptor.Get(itemId) == null)
        {
            return;
        }

        var remaining = quantity;
        while (remaining > 0)
        {
            var stackAmount = (int)Math.Min(remaining, int.MaxValue);
            var deliverable = GetDeliverableQuantity(player, itemId, stackAmount);
            if (deliverable > 0)
            {
                player.TryGiveItem(itemId, deliverable, ItemHandling.Normal);
            }

            var remainder = stackAmount - deliverable;
            if (remainder > 0)
            {
                pendingAttachments.Add(new MailAttachment
                {
                    ItemId = itemId,
                    Quantity = remainder
                });
            }

            remaining -= stackAmount;
        }
    }

    private static int GetDeliverableQuantity(Player player, Guid itemId, int quantity)
    {
        if (quantity <= 0 || player == null)
        {
            return 0;
        }

        if (player.CanGiveItem(itemId, quantity))
        {
            return quantity;
        }

        var low = 0;
        var high = quantity;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            if (player.CanGiveItem(itemId, mid))
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    private static void SendRewardMail(
        Player player,
        AchievementDescriptor achievement,
        List<MailAttachment> attachments
    )
    {
        var mail = new MailBox(
            sender: player,
            receiver: player,
            title: Strings.Achievements.Title,
            message: Strings.Achievements.CompletedNotification.ToString(achievement.Name),
            attachments: attachments
        );

        player.MailBoxs.Add(mail);
        PacketSender.SendOpenMailBox(player);
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

        return achievement.Category == AchievementCategory.Quests;
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

        return achievement.Category == AchievementCategory.Exploration;
    }

    private static bool MatchesItemTrigger(AchievementDescriptor achievement, Guid? itemId)
    {
        var hasItemCondition = ContainsCondition<HasItemCondition>(achievement.Requirements);
        if (hasItemCondition)
        {
            return MatchesItemCondition(achievement.Requirements, itemId);
        }

        return achievement.Category == AchievementCategory.Professions;
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

    private sealed class AchievementIndex
    {
        public static AchievementIndex Empty { get; } = new(
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            new Dictionary<Guid, List<AchievementDescriptor>>(),
            new Dictionary<Guid, List<AchievementDescriptor>>(),
            new Dictionary<Guid, List<AchievementDescriptor>>(),
            new Dictionary<MapZone, List<AchievementDescriptor>>(),
            new Dictionary<Guid, List<AchievementDescriptor>>()
        );

        private AchievementIndex(
            List<AchievementDescriptor> metaAchievements,
            List<AchievementDescriptor> levelUp,
            List<AchievementDescriptor> npcKill,
            List<AchievementDescriptor> dungeonCompleted,
            List<AchievementDescriptor> questGeneral,
            List<AchievementDescriptor> mapGeneral,
            List<AchievementDescriptor> itemGeneral,
            Dictionary<Guid, List<AchievementDescriptor>> questById,
            Dictionary<Guid, List<AchievementDescriptor>> itemById,
            Dictionary<Guid, List<AchievementDescriptor>> mapById,
            Dictionary<MapZone, List<AchievementDescriptor>> mapByZoneType,
            Dictionary<Guid, List<AchievementDescriptor>> metaByAchievementId
        )
        {
            MetaAchievements = metaAchievements;
            LevelUp = levelUp;
            NpcKill = npcKill;
            DungeonCompleted = dungeonCompleted;
            QuestGeneral = questGeneral;
            MapGeneral = mapGeneral;
            ItemGeneral = itemGeneral;
            QuestById = questById;
            ItemById = itemById;
            MapById = mapById;
            MapByZoneType = mapByZoneType;
            MetaByAchievementId = metaByAchievementId;
        }

        public IReadOnlyList<AchievementDescriptor> MetaAchievements { get; }

        public IReadOnlyList<AchievementDescriptor> LevelUp { get; }

        public IReadOnlyList<AchievementDescriptor> NpcKill { get; }

        public IReadOnlyList<AchievementDescriptor> DungeonCompleted { get; }

        public IReadOnlyList<AchievementDescriptor> QuestGeneral { get; }

        public IReadOnlyList<AchievementDescriptor> MapGeneral { get; }

        public IReadOnlyList<AchievementDescriptor> ItemGeneral { get; }

        public IReadOnlyDictionary<Guid, List<AchievementDescriptor>> QuestById { get; }

        public IReadOnlyDictionary<Guid, List<AchievementDescriptor>> ItemById { get; }

        public IReadOnlyDictionary<Guid, List<AchievementDescriptor>> MapById { get; }

        public IReadOnlyDictionary<MapZone, List<AchievementDescriptor>> MapByZoneType { get; }

        public IReadOnlyDictionary<Guid, List<AchievementDescriptor>> MetaByAchievementId { get; }

        public static AchievementIndex Build()
        {
            var metaAchievements = new List<AchievementDescriptor>();
            var levelUp = new List<AchievementDescriptor>();
            var npcKill = new List<AchievementDescriptor>();
            var dungeonCompleted = new List<AchievementDescriptor>();
            var questGeneral = new List<AchievementDescriptor>();
            var mapGeneral = new List<AchievementDescriptor>();
            var itemGeneral = new List<AchievementDescriptor>();
            var questById = new Dictionary<Guid, HashSet<AchievementDescriptor>>();
            var itemById = new Dictionary<Guid, HashSet<AchievementDescriptor>>();
            var mapById = new Dictionary<Guid, HashSet<AchievementDescriptor>>();
            var mapByZoneType = new Dictionary<MapZone, HashSet<AchievementDescriptor>>();
            var metaByAchievementId = new Dictionary<Guid, HashSet<AchievementDescriptor>>();

            foreach (var achievement in AchievementDescriptor.Lookup.Values.OfType<AchievementDescriptor>())
            {
                if (achievement.MetaAchievementIds.Count > 0)
                {
                    metaAchievements.Add(achievement);
                    foreach (var achievementId in achievement.MetaAchievementIds)
                    {
                        AddToIndex(metaByAchievementId, achievementId, achievement);
                    }
                    continue;
                }

                if (achievement.Category == AchievementCategory.Events ||
                    HasLevelRequirement(achievement.Requirements))
                {
                    levelUp.Add(achievement);
                }

                if (achievement.Category == AchievementCategory.Monsters)
                {
                    npcKill.Add(achievement);
                }

                if (achievement.Category == AchievementCategory.Dungeons)
                {
                    dungeonCompleted.Add(achievement);
                }

                var questConditionIds = GetQuestConditionIds(achievement.Requirements);
                if (questConditionIds.Count > 0)
                {
                    foreach (var questId in questConditionIds)
                    {
                        AddToIndex(questById, questId, achievement);
                    }
                }
                else if (achievement.Category == AchievementCategory.Quests)
                {
                    questGeneral.Add(achievement);
                }

                var itemConditionIds = GetItemConditionIds(achievement.Requirements);
                if (itemConditionIds.Count > 0)
                {
                    foreach (var itemId in itemConditionIds)
                    {
                        AddToIndex(itemById, itemId, achievement);
                    }
                }
                else if (achievement.Category == AchievementCategory.Professions)
                {
                    itemGeneral.Add(achievement);
                }

                var mapConditionIds = GetMapConditionIds(achievement.Requirements);
                var zoneConditionTypes = GetZoneConditionTypes(achievement.Requirements);
                if (mapConditionIds.Count > 0 || zoneConditionTypes.Count > 0)
                {
                    foreach (var mapId in mapConditionIds)
                    {
                        AddToIndex(mapById, mapId, achievement);
                    }

                    foreach (var zoneType in zoneConditionTypes)
                    {
                        AddToIndex(mapByZoneType, zoneType, achievement);
                    }
                }
                else if (achievement.Category == AchievementCategory.Exploration)
                {
                    mapGeneral.Add(achievement);
                }
            }

            return new AchievementIndex(
                metaAchievements,
                levelUp,
                npcKill,
                dungeonCompleted,
                questGeneral,
                mapGeneral,
                itemGeneral,
                ToListIndex(questById),
                ToListIndex(itemById),
                ToListIndex(mapById),
                ToListIndex(mapByZoneType),
                ToListIndex(metaByAchievementId)
            );
        }

        private static void AddToIndex<TKey>(
            Dictionary<TKey, HashSet<AchievementDescriptor>> index,
            TKey key,
            AchievementDescriptor achievement
        ) where TKey : notnull
        {
            if (!index.TryGetValue(key, out var achievements))
            {
                achievements = new HashSet<AchievementDescriptor>();
                index[key] = achievements;
            }

            achievements.Add(achievement);
        }

        private static Dictionary<TKey, List<AchievementDescriptor>> ToListIndex<TKey>(
            Dictionary<TKey, HashSet<AchievementDescriptor>> index
        ) where TKey : notnull
        {
            return index.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
        }

        private static HashSet<Guid> GetQuestConditionIds(ConditionLists requirements)
        {
            return requirements.Lists
                .SelectMany(list => list.Conditions.OfType<QuestCompletedCondition>())
                .Select(condition => condition.QuestId)
                .ToHashSet();
        }

        private static HashSet<Guid> GetItemConditionIds(ConditionLists requirements)
        {
            return requirements.Lists
                .SelectMany(list => list.Conditions.OfType<HasItemCondition>())
                .Select(condition => condition.ItemId)
                .ToHashSet();
        }

        private static HashSet<Guid> GetMapConditionIds(ConditionLists requirements)
        {
            return requirements.Lists
                .SelectMany(list => list.Conditions.OfType<MapIsCondition>())
                .Select(condition => condition.MapId)
                .ToHashSet();
        }

        private static HashSet<MapZone> GetZoneConditionTypes(ConditionLists requirements)
        {
            return requirements.Lists
                .SelectMany(list => list.Conditions.OfType<MapZoneTypeIs>())
                .Select(condition => condition.ZoneType)
                .ToHashSet();
        }
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
