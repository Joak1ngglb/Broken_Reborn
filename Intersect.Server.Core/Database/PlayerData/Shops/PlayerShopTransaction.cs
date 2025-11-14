using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Server.Entities;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Shops;

public class PlayerShopTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public virtual PlayerShop Shop { get; set; }

    [Required]
    public Guid ShopItemId { get; set; }

    [ForeignKey(nameof(ShopItemId))]
    public virtual PlayerShopItem ShopItem { get; set; }

    [Required]
    public Guid OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public virtual Player Owner { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public int UnitPrice { get; set; }

    [Required]
    public int TotalPrice { get; set; }

    [Required]
    public string BuyerName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public ItemProperties ItemProperties { get; set; } = new();

    [JsonIgnore]
    [Column("ItemProperties")]
    public string ItemPropertiesJson
    {
        get => JsonConvert.SerializeObject(ItemProperties);
        set => ItemProperties = string.IsNullOrWhiteSpace(value)
            ? new ItemProperties()
            : JsonConvert.DeserializeObject<ItemProperties>(value) ?? new ItemProperties();
    }
}
