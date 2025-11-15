using System;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject]
public partial class GiveItemAction : AdminAction
{
    public GiveItemAction()
    {

    }

    public GiveItemAction(string name, Guid itemId, int quantity, bool allowBankOverflow)
    {
        Name = name;
        ItemId = itemId;
        Quantity = quantity;
        AllowBankOverflow = allowBankOverflow;
    }

    [Key(1)]
    public override Enums.AdminAction Action { get; } = Enums.AdminAction.GiveItem;

    [Key(2)]
    public string Name { get; set; } = string.Empty;

    [Key(3)]
    public Guid ItemId { get; set; }

    [Key(4)]
    public int Quantity { get; set; }

    [Key(5)]
    public bool AllowBankOverflow { get; set; }
}
