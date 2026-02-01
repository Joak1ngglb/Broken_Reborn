using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public enum ProgressMode
{
    Binary,
    Quantitative,
}

public sealed class ObjectiveProgress
{
    public ObjectiveProgress()
    {
    }

    public ObjectiveProgress(int current, int target, ProgressMode? mode = null)
    {
        Current = current;
        Target = target;
        Mode = mode;
    }

    public int Current { get; set; }

    public int Target { get; set; }

    public ProgressMode? Mode { get; set; }

    [JsonIgnore]
    public bool IsCompleted => Target > 0 ? Current >= Target : Current > 0;
}
