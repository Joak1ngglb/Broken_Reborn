using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Entities;
using Intersect.Framework.Core.Entities;

namespace Intersect.Server.Database.PlayerData.Shops;

public class PlayerShop : IPlayerOwned
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public virtual Player Owner { get; set; }

    [Required]
    public Guid MapId { get; set; }

    [Required]
    public Guid MapInstanceId { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Z { get; set; }

    public string Title { get; set; } = string.Empty;

    public PlayerShopStatus Status { get; set; } = PlayerShopStatus.Active;

    public long PendingGold { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string Decoration { get; set; } = PlayerShopEntityConstants.DefaultDecoration;

    public virtual List<PlayerShopItem> Items { get; set; } = new();

    public virtual List<PlayerShopTransaction> Transactions { get; set; } = new();

    Player IPlayerOwned.Player => Owner;

    Guid IPlayerOwned.PlayerId => OwnerId;
}
