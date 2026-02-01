using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public partial class AchievementProgress
{
    public int Progress;

    public bool Completed;

    public DateTime? CompletedAt;

    public List<ObjectiveProgress> Objectives { get; set; } = [];

    public AchievementProgress()
    {
    }

    public AchievementProgress(string? data)
    {
        var progress = FromJson(data);
        Progress = progress.Progress;
        Completed = progress.Completed;
        CompletedAt = progress.CompletedAt;
        Objectives = progress.Objectives;
    }

    public static AchievementProgress FromJson(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return new AchievementProgress();
        }

        return JsonConvert.DeserializeObject<AchievementProgress>(data) ?? new AchievementProgress();
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}
