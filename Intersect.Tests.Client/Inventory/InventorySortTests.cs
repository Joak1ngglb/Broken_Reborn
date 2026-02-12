using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Intersect;
using Intersect.Client.Framework.Items;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Utilities;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using NUnit.Framework;

namespace Intersect.Tests.Client.Inventory;

[TestFixture]
public class InventorySortTests
{
    private sealed class TestItem : IItem
    {
        private readonly ItemDescriptor? _descriptor;

        public TestItem(Guid itemId, int quantity, ItemDescriptor descriptor)
        {
            ItemId = itemId;
            Quantity = quantity;
            _descriptor = descriptor;
        }

        public Guid? BagId { get; set; }

        public ItemDescriptor Descriptor => _descriptor!;

        public Guid ItemId { get; set; }

        public int Quantity { get; set; }

        public ItemProperties ItemProperties { get; set; }

        public void Load(Guid id, int quantity, Guid? bagId, ItemProperties itemProperties)
        {
            ItemId = id;
            Quantity = quantity;
            BagId = bagId;
            ItemProperties = itemProperties;
        }
    }

    [Test]
    public void FilterAndSort_SkipsInvalidItems()
    {
        var ensure = typeof(Options).GetMethod("EnsureCreated", BindingFlags.NonPublic | BindingFlags.Static);
        ensure!.Invoke(null, null);
        Options.Instance.Items.ItemSubtypes = new Dictionary<ItemType, List<string>>
        {
            { ItemType.Equipment, new() { "Sword" } }
        };

        var validA = new ItemDescriptor { Name = "A", ItemType = ItemType.Equipment, Subtype = "Sword", Price = 1 };
        var invalid = new ItemDescriptor { Name = string.Empty, ItemType = ItemType.Equipment, Subtype = "Sword", Price = 1 };
        var validB = new ItemDescriptor { Name = "B", ItemType = ItemType.Equipment, Subtype = "Sword", Price = 1 };

        var items = new[] { validA, invalid, validB };

        var sorted = ItemListHelper.FilterAndSort(
            items,
            d => d,
            d => 1,
            searchText: null,
            type: null,
            subtype: null,
            criterion: SortCriterion.Name,
            ascending: true
        ).ToList();

        Assert.That(sorted, Is.EqualTo(new[] { validA, validB }));
    }

    [Test]
    public void QuantitySortPlan_PreservesStackTotalsAndDistribution()
    {
        var ensure = typeof(Options).GetMethod("EnsureCreated", BindingFlags.NonPublic | BindingFlags.Static);
        ensure!.Invoke(null, null);
        Options.Instance.Items.ItemSubtypes = new Dictionary<ItemType, List<string>>
        {
            { ItemType.Consumable, new() { "Potion" } },
        };

        var descriptorA = new ItemDescriptor { Name = "Potion A", ItemType = ItemType.Consumable, Subtype = "Potion" };
        var descriptorB = new ItemDescriptor { Name = "Potion B", ItemType = ItemType.Consumable, Subtype = "Potion" };

        var itemAId = Guid.NewGuid();
        var itemBId = Guid.NewGuid();

        var inventory = new IItem[]
        {
            new TestItem(itemAId, 8, descriptorA),
            null!,
            new TestItem(itemAId, 3, descriptorA),
            new TestItem(itemBId, 7, descriptorB),
            new TestItem(itemAId, 1, descriptorA),
        };

        var planMethod = typeof(InventoryWindow).GetMethod(
            "BuildSortSwapPlan",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
        );

        Assert.That(planMethod, Is.Not.Null);
        var swaps = (IReadOnlyList<(int from, int to)>)planMethod!.Invoke(
            null,
            [inventory, inventory.Length, SortCriterion.Quantity, true]
        )!;

        var originalStacks = inventory.Where(slot => slot?.Descriptor != null)
            .Select(slot => (slot.ItemId, slot.Quantity))
            .OrderBy(stack => stack.ItemId)
            .ThenBy(stack => stack.Quantity)
            .ToList();
        var originalTotal = originalStacks.Sum(stack => stack.Quantity);

        foreach (var (from, to) in swaps)
        {
            (inventory[from], inventory[to]) = (inventory[to], inventory[from]);
        }

        var reorderedStacks = inventory.Where(slot => slot?.Descriptor != null)
            .Select(slot => (slot.ItemId, slot.Quantity))
            .OrderBy(stack => stack.ItemId)
            .ThenBy(stack => stack.Quantity)
            .ToList();
        var reorderedTotal = reorderedStacks.Sum(stack => stack.Quantity);

        Assert.That(reorderedTotal, Is.EqualTo(originalTotal));
        Assert.That(reorderedStacks, Is.EqualTo(originalStacks));

        var orderedQuantities = inventory.Where(slot => slot?.Descriptor != null)
            .Select(slot => slot.Quantity)
            .ToArray();
        Assert.That(orderedQuantities, Is.EqualTo(new[] { 1, 3, 7, 8 }));
    }
}
