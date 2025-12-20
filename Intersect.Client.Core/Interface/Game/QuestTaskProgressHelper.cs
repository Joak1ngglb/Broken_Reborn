using System;
using System.Collections.Generic;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game;

public static class QuestTaskProgressHelper
{
    public static bool IsTaskCompleted(
        QuestDescriptor? quest,
        QuestTaskDescriptor? task,
        IReadOnlyDictionary<Guid, QuestProgress>? progressLookup)
    {
        if (quest == null || task == null || progressLookup == null)
        {
            return false;
        }

        if (!progressLookup.TryGetValue(quest.Id, out var progress))
        {
            return false;
        }

        if (progress.Completed)
        {
            return true;
        }

        var currentIndex = quest.GetTaskIndex(progress.TaskId);
        var taskIndex = quest.GetTaskIndex(task.Id);

        return currentIndex > taskIndex;
    }

    public static bool IsCurrentTask(
        QuestDescriptor? quest,
        QuestTaskDescriptor? task,
        IReadOnlyDictionary<Guid, QuestProgress>? progressLookup)
    {
        if (quest == null || task == null || progressLookup == null)
        {
            return false;
        }

        if (!progressLookup.TryGetValue(quest.Id, out var progress))
        {
            return false;
        }

        if (progress.Completed)
        {
            return false;
        }

        return quest.GetTaskIndex(progress.TaskId) == quest.GetTaskIndex(task.Id);
    }

    public static int GetTaskProgress(
        QuestDescriptor? quest,
        QuestTaskDescriptor? task,
        IReadOnlyDictionary<Guid, QuestProgress>? progressLookup)
    {
        if (quest == null || task == null || progressLookup == null)
        {
            return 0;
        }

        if (!progressLookup.TryGetValue(quest.Id, out var progress))
        {
            return 0;
        }

        var currentIndex = quest.GetTaskIndex(progress.TaskId);
        var taskIndex = quest.GetTaskIndex(task.Id);

        if (progress.Completed || currentIndex > taskIndex)
        {
            return task.Quantity;
        }

        if (currentIndex == taskIndex)
        {
            return progress.TaskProgress;
        }

        return 0;
    }
}
