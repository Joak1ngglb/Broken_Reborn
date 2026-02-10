using System.ComponentModel.DataAnnotations.Schema;
using FrameworkAchievementProgress = Intersect.Framework.Core.GameObjects.Achievements.AchievementProgress;
using Intersect.Server.Entities;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Players;

public partial class AchievementProgress : IPlayerOwned
{
    public AchievementProgress()
    {
    }

    public AchievementProgress(Guid id)
    {
        AchievementId = id;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public Guid Id { get; private set; }

    [JsonIgnore]
    public Guid AchievementId { get; private set; }

    public int Progress { get; set; }

    public bool Completed { get; set; }

    public DateTime? CompletedAt { get; set; }

    [JsonIgnore]
    public Guid PlayerId { get; private set; }

    [JsonIgnore]
    [ForeignKey(nameof(PlayerId))]
    public virtual Player Player { get; private set; }

    public string Data()
    {
        var progress = new FrameworkAchievementProgress
        {
            Progress = Progress,
            Completed = Completed,
            CompletedAt = CompletedAt,
            Objectives = Objectives
        };

        return progress.ToJson();
    }
}
