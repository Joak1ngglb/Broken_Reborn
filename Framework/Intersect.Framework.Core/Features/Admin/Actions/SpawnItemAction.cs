using System;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject]
public partial class SpawnItemAction : AdminAction
{
    public SpawnItemAction()
    {

    }

    public SpawnItemAction(string name, Guid itemId, int quantity, bool reserveForTarget)
    {
        Name = name;
        ItemId = itemId;
        Quantity = quantity;
        ReserveForTarget = reserveForTarget;
    }

    [Key(1)]
    public override Enums.AdminAction Action { get; } = Enums.AdminAction.SpawnItem;

    [Key(2)]
    public string Name { get; set; } = string.Empty;

    [Key(3)]
    public Guid ItemId { get; set; }

    [Key(4)]
    public int Quantity { get; set; }

    [Key(5)]
    public bool ReserveForTarget { get; set; }
}
