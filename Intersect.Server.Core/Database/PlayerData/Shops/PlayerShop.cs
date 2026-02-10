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

    /// <summary>
    /// Helper column used to enforce uniqueness only for active shops.
    /// 1 = active, null = inactive/closed.
    /// </summary>
    public int? ActiveUniquenessToken { get; set; }

    public long PendingGold { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Set once expired-shop liquidation (gold payout + unsold return) has been applied.
    /// Null means liquidation is still pending.
    /// </summary>
    public DateTime? LiquidatedAt { get; set; }

    /// <summary>
    /// Audit value storing how much pending gold was paid during liquidation.
    /// </summary>
    public long LiquidatedGold { get; set; }

    public string Decoration { get; set; } = PlayerShopEntityConstants.DefaultDecoration;

    public virtual List<PlayerShopItem> Items { get; set; } = new();

    public virtual List<PlayerShopTransaction> Transactions { get; set; } = new();

    Player IPlayerOwned.Player => Owner;

    Guid IPlayerOwned.PlayerId => OwnerId;
}
