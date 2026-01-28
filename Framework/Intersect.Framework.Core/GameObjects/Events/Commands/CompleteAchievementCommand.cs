namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class CompleteAchievementCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.CompleteAchievement;

    public Guid AchievementId { get; set; }
}
