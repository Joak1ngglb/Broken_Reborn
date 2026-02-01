using System.Collections.Generic;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Achievements;

[MessagePackObject]
public sealed class AchievementProgressDto
{
    public const int CurrentSchemaVersion = 1;

    [Key(0)]
    public int Progress { get; set; }

    [Key(1)]
    public bool Completed { get; set; }

    [Key(2)]
    public long? CompletedAtTicks { get; set; }

    [Key(3)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [Key(4)]
    public List<ObjectiveProgress> Objectives { get; set; } = [];
}
