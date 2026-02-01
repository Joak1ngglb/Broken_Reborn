using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public partial class AchievementProgress
{
    public int Progress;

    public bool Completed;

    public DateTime? CompletedAt;

    public List<ObjectiveProgress> Objectives { get; set; } = [];

    public AchievementProgress(string data)
    {
        JsonConvert.PopulateObject(data, this);
    }
}
