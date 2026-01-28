using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public partial class AchievementProgress
{
    public int Progress;

    public bool Completed;

    public DateTime? CompletedAt;

    public AchievementProgress(string data)
    {
        JsonConvert.PopulateObject(data, this);
    }
}
