using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Items;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Shops;

public class PlayerShopItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public virtual PlayerShop Shop { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public int PricePerUnit { get; set; }

    public bool IsSold { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SoldAt { get; set; }

    [NotMapped]
    public ItemProperties Properties { get; set; } = new();

    [JsonIgnore]
    [Column("ItemProperties")]
    public string ItemPropertiesJson
    {
        get => JsonConvert.SerializeObject(Properties);
        set => Properties = string.IsNullOrWhiteSpace(value)
            ? new ItemProperties()
            : JsonConvert.DeserializeObject<ItemProperties>(value) ?? new ItemProperties();
    }
}
