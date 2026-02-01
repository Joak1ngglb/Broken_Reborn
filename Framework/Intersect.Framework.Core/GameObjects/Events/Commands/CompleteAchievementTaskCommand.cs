namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class CompleteAchievementTaskCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.CompleteAchievementTask;

    public Guid AchievementId { get; set; }

    public int ObjectiveIndex { get; set; } = -1;
}
