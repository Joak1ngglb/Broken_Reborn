using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class UnequipItemPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public UnequipItemPacket()
    {
    }

    public UnequipItemPacket(int slot, Guid? itemId = null)
    {
        Slot = slot;
        ItemId = itemId;
    }

    [Key(0)]
    public int Slot { get; set; }

    [Key(1)]
    public Guid? ItemId { get; set; }

}
