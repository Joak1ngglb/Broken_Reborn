using MessagePack;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public enum ProgressMode
{
    Binary,
    Quantitative,
}

[MessagePackObject]
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

    [Key(0)]
    public int Current { get; set; }

    [Key(1)]
    public int Target { get; set; }

    [Key(2)]
    public ProgressMode? Mode { get; set; }

    [JsonIgnore]
    [Key(3)]
    public bool IsCompleted => Target > 0 ? Current >= Target : Current > 0;
}
