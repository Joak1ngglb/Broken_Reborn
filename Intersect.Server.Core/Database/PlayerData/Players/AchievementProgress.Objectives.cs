using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Achievements;

namespace Intersect.Server.Database.PlayerData.Players;

public partial class AchievementProgress
{
    [NotMapped]
    public List<ObjectiveProgress> Objectives { get; set; } = [];
}
