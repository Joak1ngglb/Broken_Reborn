using System;
using System.Collections.Generic;
using System.Linq;
using Intersect;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.GameObjects;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Quests;

namespace Intersect.Client.Interface.Game;

public class QuestTrackerWindow
{
    private readonly WindowControl _window;

    private readonly TreeControl _questTree;

    private readonly HashSet<Guid> _expandedQuests = new();

    private bool _shouldRefresh = true;

    public QuestTrackerWindow(Canvas gameCanvas)
    {
        _window = new WindowControl(gameCanvas, Strings.QuestLog.Title, false, "QuestTrackerWindow");
        _window.DisableResizing();

        _questTree = new TreeControl(_window, "QuestTrackerTree")
        {
            Dock = Pos.Fill,
        };

        _window.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    public event Action<Guid>? QuestSelected;

    public void Update(bool shouldRefresh)
    {
        _shouldRefresh |= shouldRefresh;

        if (!_window.IsVisibleInTree)
        {
            return;
        }

        if (_shouldRefresh || _window.IsHidden)
        {
            RefreshQuests();
            _shouldRefresh = false;
        }
    }

    public void NotifyQuestProgressUpdated(IEnumerable<Guid> questIds)
    {
        if (questIds == null)
        {
            return;
        }

        _shouldRefresh = true;

        if (_window.IsHidden || !_window.IsVisibleInTree)
        {
            return;
        }

        RefreshQuests();
        _shouldRefresh = false;
    }

    private void RefreshQuests()
    {
        _questTree.RemoveAll();

        var progressLookup = Globals.Me?.QuestProgress;
        if (progressLookup == null || progressLookup.Count == 0)
        {
            return;
        }

        var quests = progressLookup
            .Where(kv => kv.Value.TaskId != Guid.Empty && QuestDescriptor.TryGet(kv.Key, out _))
            .Select(kv => new { Quest = QuestDescriptor.Get(kv.Key), Progress = kv.Value })
            .Where(pair => pair.Quest != null)
            .OrderBy(pair => pair.Quest?.OrderValue ?? 0)
            .ToList();

        if (quests.Count == 0)
        {
            _window.IsHidden = true;
            return;
        }

        _window.IsHidden = false;

        foreach (var entry in quests)
        {
            if (entry.Quest == null)
            {
                continue;
            }

            var questNode = _questTree.AddNode(BuildQuestHeader(entry.Quest, entry.Progress), entry.Quest.Id);
            questNode.TextColor = entry.Progress.Completed
                ? CustomColors.QuestWindow.Completed
                : CustomColors.QuestWindow.InProgress;

            questNode.LabelPressed += (_, _) => ToggleQuestExpansion(questNode, entry.Quest.Id);
            questNode.DoubleClicked += (_, _) => QuestSelected?.Invoke(entry.Quest.Id);

            foreach (var task in entry.Quest.Tasks)
            {
                var taskNode = questNode.AddNode(BuildTaskText(entry.Quest, task), task.Id);
                taskNode.IsSelectable = false;
                taskNode.TextColor = GetTaskColor(entry.Quest, task);
            }

            if (_expandedQuests.Contains(entry.Quest.Id))
            {
                questNode.Open();
            }
        }
    }

    private void ToggleQuestExpansion(TreeNode node, Guid questId)
    {
        if (_expandedQuests.Contains(questId))
        {
            _expandedQuests.Remove(questId);
            node.Close();
        }
        else
        {
            _expandedQuests.Add(questId);
            node.Open();
        }
    }

    private string BuildQuestHeader(QuestDescriptor quest, QuestProgress progress)
    {
        var currentTask = quest.FindTask(progress.TaskId);
        var taskText = currentTask == null
            ? Strings.QuestLog.CurrentTask.ToString()
            : BuildTaskText(quest, currentTask);

        return $"{quest.Name} - {taskText}";
    }

    private string BuildTaskText(QuestDescriptor quest, QuestTaskDescriptor task)
    {
        var currentProgress = QuestTaskProgressHelper.GetTaskProgress(quest, task, Globals.Me?.QuestProgress);

        return task.Objective switch
        {
            QuestObjective.GatherItems => Strings.QuestLog.TaskItem.ToString(
                currentProgress,
                task.Quantity,
                ItemDescriptor.GetName(task.TargetId)
            ),
            QuestObjective.KillNpcs => Strings.QuestLog.TaskNpc.ToString(
                currentProgress,
                task.Quantity,
                NPCDescriptor.GetName(task.TargetId)
            ),
            _ => string.IsNullOrWhiteSpace(task.Description) ? Strings.QuestLog.CurrentTask.ToString() : task.Description
        };
    }

    private Color GetTaskColor(QuestDescriptor quest, QuestTaskDescriptor task)
    {
        if (QuestTaskProgressHelper.IsTaskCompleted(quest, task, Globals.Me?.QuestProgress))
        {
            return CustomColors.QuestWindow.Completed;
        }

        if (QuestTaskProgressHelper.IsCurrentTask(quest, task, Globals.Me?.QuestProgress))
        {
            return CustomColors.QuestWindow.InProgress;
        }

        return CustomColors.QuestWindow.NotStarted;
    }
}
