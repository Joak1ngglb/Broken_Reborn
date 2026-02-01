using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Server.Entities;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Players;

[Table("Player_Stats")]
public class PlayerStats : IPlayerOwned
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore]
    public Guid PlayerId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(PlayerId))]
    public Player Player { get; set; }

    public int PvPKills { get; set; }

    public int Deaths { get; set; }

    public int JobsLevel { get; set; }

    public int Crafts { get; set; }

    public int Harvests { get; set; }

    public void EnsureOwner(Player player)
    {
        if (player == null)
        {
            return;
        }

        Player = player;
        if (PlayerId == Guid.Empty)
        {
            PlayerId = player.Id;
        }
    }
}
