using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public sealed class Corpse : Entity
{
    public Corpse(Player player, long despawnTime) : base(Guid.NewGuid(), null, EntityType.GlobalEntity)
    {
        OwnerId = player.Id;
        DespawnTime = despawnTime;
        MapId = player.MapId;
        Position = player.Position;
        DirectionFacing = player.DirectionFacing;
        Sprite = player.Sprite;
        Color = player.Color;
        Face = player.Face;
        Gender = player.Gender;
        Name = player.Name;
        HideName = true;
        Passable = true;
        IsDead = true;
        mRenderPriority = 0;
        Equipment = BuildEquipmentSnapshot(player);
    }

    public Guid OwnerId { get; }

    public long DespawnTime { get; }

    public override bool CanBeAttacked => false;

    public override bool ShouldDrawName => false;

    protected override bool ShouldDrawHpBar => false;

    public override bool Update()
    {
        if (DespawnTime > 0 && Timing.Global.Milliseconds >= DespawnTime)
        {
            Globals.RemoveCorpse(OwnerId);
            return false;
        }

        IsMoving = false;
        OffsetX = 0;
        OffsetY = 0;

        return base.Update();
    }

    private static Dictionary<int, List<Guid>> CloneEquipment(Dictionary<int, List<Guid>> source)
    {
        return source.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());
    }

    private static Dictionary<int, List<Guid>> BuildEquipmentSnapshot(Player player)
    {
        if (player != Globals.Me)
        {
            return CloneEquipment(player.Equipment);
        }

        var result = new Dictionary<int, List<Guid>>();
        for (var slot = 0; slot < Options.Instance.Equipment.Slots.Count; slot++)
        {
            if (!player.MyEquipment.TryGetValue(slot, out var inventorySlots))
            {
                result[slot] = [];
                continue;
            }

            result[slot] = inventorySlots
                .Where(index => index >= 0 && index < player.Inventory.Length)
                .Select(index => player.Inventory[index]?.ItemId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
        }

        return result;
    }
}
