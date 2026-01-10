using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.Config;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Network.Packets.Server;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Framework.Items;
using Newtonsoft.Json;

namespace Intersect.Server.Database;

public class Item : IItem
{
    [JsonIgnore][NotMapped] public double DropChance { get; set; } = 100;

    public Item()
    {
        Properties = new ItemProperties();
    }

    public Item(Guid itemId, int quantity, ItemProperties properties = null) : this(
        itemId,
        quantity,
        null,
        null,
        properties
    )
    {
    }

    public Item(
        Guid itemId,
        int quantity,
        Guid? bagId,
        Bag bag,
        ItemProperties properties = null
    )
    {
        ItemId = itemId;
        Quantity = quantity;
        BagId = bagId;
        Bag = bag;
        Properties = properties ?? new ItemProperties();

        if (!ItemDescriptor.TryGet(itemId, out var descriptor) || properties != null)
        {
            return;
        }

        if (descriptor.ItemType != ItemType.Equipment)
        {
            return;
        }

        foreach (Stat stat in Enum.GetValues<Stat>())
        {
            if (descriptor.TryGetRangeFor(stat, out var range))
            {
                Properties.StatModifiers[(int)stat] = range.Roll();
            }
        }

        foreach (Vital vital in Enum.GetValues<Vital>())
        {
            if (descriptor.TryGetRangeFor(vital, out var range))
            {
                Properties.VitalModifiers[(int)vital] = range.Roll();
            }
        }

        foreach (ItemEffect effect in Enum.GetValues<ItemEffect>())
        {
            if (effect == ItemEffect.None)
            {
                continue;
            }

            if (!descriptor.TryGetRangeFor(effect, out var range))
            {
                continue;
            }

            var roll = range.Roll();
            if (roll == 0)
            {
                continue;
            }

            Properties.EffectModifiers ??= new Dictionary<ItemEffect, EffectData>();
            Properties.EffectModifiers[effect] = new EffectData(effect, roll);
        }
    }

    public Item(Item item) : this(item.ItemId, item.Quantity, item.BagId, item.Bag)
    {
        Properties = new ItemProperties(item.Properties);
        DropChance = item.DropChance;
    }

    // TODO: THIS SHOULD NOT BE A NULLABLE. This needs to be fixed.
    public Guid? BagId { get; set; }

    [JsonIgnore] public virtual Bag Bag { get; set; }

    public Guid ItemId { get; set; } = Guid.Empty;

    [NotMapped] public string ItemName => ItemDescriptor.GetName(ItemId);

    public int Quantity { get; set; }

    [NotMapped] public ItemProperties Properties { get; set; }

    [Column(nameof(ItemProperties))]
    [JsonIgnore]
    public string ItemPropertiesJson
    {
        get => JsonConvert.SerializeObject(Properties);
        set =>
            Properties = JsonConvert.DeserializeObject<ItemProperties>(value ?? string.Empty) ?? new ItemProperties();
    }

    [JsonIgnore, NotMapped] public ItemDescriptor Descriptor => ItemDescriptor.Get(ItemId);

    public static Item None => new();

    public Item Clone()
    {
        return new Item(this);
    }

    public string Data()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static bool TryFindSourceSlotsForItem<TItem>(
        Guid itemDescriptorId,
        int slotHint,
        int searchQuantity,
        TItem?[] slots,
        [NotNullWhen(true)] out int[] sourceSlotIndices
    ) where TItem : Item
    {
        Debug.Assert(itemDescriptorId != default);
        Debug.Assert(slots != default);

        if (searchQuantity < 1)
        {
            sourceSlotIndices = default;
            return false;
        }

        var remainingQuantity = searchQuantity;
        List<int> compatibleSlots = new();

        if (slotHint > -1)
        {
            var slot = slots[slotHint];
            if (slot?.ItemId == itemDescriptorId)
            {
                var remainingQuantityInSlot = Math.Min(remainingQuantity, slot.Quantity);
                if (remainingQuantityInSlot > 0)
                {
                    remainingQuantity -= remainingQuantityInSlot;
                    compatibleSlots.Add(slotHint);
                }
            }
        }

        for (var slotIndex = 0; 0 < remainingQuantity && slotIndex < slots.Length; ++slotIndex)
        {
            var slot = slots[slotIndex];
            if (slotIndex == slotHint)
            {
                // If slotHint is < 0 this will never be hit
                // If slotHint is >= 0 we already accounted for the current slot in the if-block above
                continue;
            }

            if (slot?.ItemId != itemDescriptorId)
            {
                continue;
            }

            var remainingQuantityInSlot = Math.Min(remainingQuantity, slot.Quantity);
            if (remainingQuantityInSlot <= 0)
            {
                continue;
            }

            remainingQuantity -= remainingQuantityInSlot;
            compatibleSlots.Add(slotIndex);
        }

        if (remainingQuantity != 0 || compatibleSlots.Count < 1)
        {
            sourceSlotIndices = default;
            return false;
        }

        sourceSlotIndices = compatibleSlots.ToArray();
        return true;
    }

    public static TItem[] FindCompatibleSlotsForItem<TItem>(
        ItemDescriptor itemDescriptor,
        int maximumStack,
        int slotHint,
        int searchQuantity,
        TItem?[] slots,
        bool excludeEmpty = false
    ) where TItem : Item
    {
        Debug.Assert(itemDescriptor != default);
        Debug.Assert(slots != default);

        if (excludeEmpty && itemDescriptor.ItemType == ItemType.Equipment)
        {
            return Array.Empty<TItem>();
        }

        var availableQuantity = 0;
        List<TItem> compatibleSlots = new();

        if (slotHint > -1)
        {
            var slot = slots[slotHint];
            if (slot == null || slot.ItemId == default)
            {
                if (!excludeEmpty)
                {
                    availableQuantity += maximumStack;
                    compatibleSlots.Add(slot);
                }
            }
            else if (slot.ItemId == itemDescriptor.Id)
            {
                var availableQuantityInSlot = maximumStack - slot.Quantity;
                if (availableQuantityInSlot > 0)
                {
                    availableQuantity += availableQuantityInSlot;
                    compatibleSlots.Add(slot);
                }
            }
        }

        for (var slotIndex = 0; availableQuantity < searchQuantity && slotIndex < slots.Length; ++slotIndex)
        {
            var slot = slots[slotIndex];
            if (slotIndex == slotHint)
            {
                // If slotHint is < 0 this will never be hit
                // If slotHint is >= 0 we already accounted for the current slot in the if-block above
                continue;
            }

            if (slot == null || slot.ItemId == default)
            {
                if (excludeEmpty)
                {
                    continue;
                }

                availableQuantity += maximumStack;
                compatibleSlots.Add(slot);
                continue;
            }

            if (itemDescriptor.ItemType == ItemType.Equipment)
            {
                // Equipment slots are not valid target slots because they can have randomized stats
                continue;
            }

            if (slot.ItemId != itemDescriptor.Id)
            {
                continue;
            }

            var availableQuantityInSlot = maximumStack - slot.Quantity;
            if (availableQuantityInSlot <= 0)
            {
                continue;
            }

            availableQuantity += availableQuantityInSlot;
            compatibleSlots.Add(slot);
        }

        return compatibleSlots.ToArray();
    }

    public static int[] FindCompatibleSlotsForItem<TItem>(
        Guid itemDescriptorId,
        ItemType itemType,
        int maximumStack,
        int slotHint,
        int searchQuantity,
        TItem?[] slots
    ) where TItem : Item
    {
        Debug.Assert(itemDescriptorId != default);
        Debug.Assert(slots != default);

        var availableQuantity = 0;
        List<int> compatibleSlots = new();

        if (slotHint > -1)
        {
            var slot = slots[slotHint];
            if (slot == null || slot.ItemId == default)
            {
                availableQuantity += maximumStack;
                compatibleSlots.Add(slotHint);
            }
            else if (slot.ItemId == itemDescriptorId)
            {
                var availableQuantityInSlot = maximumStack - slot.Quantity;
                if (availableQuantityInSlot > 0)
                {
                    availableQuantity += availableQuantityInSlot;
                    compatibleSlots.Add(slotHint);
                }
            }
        }

        for (var slotIndex = 0; availableQuantity < searchQuantity && slotIndex < slots.Length; ++slotIndex)
        {
            var slot = slots[slotIndex];
            if (slotIndex == slotHint)
            {
                // If slotHint is < 0 this will never be hit
                // If slotHint is >= 0 we already accounted for the current slot in the if-block above
                continue;
            }

            if (slot == null || slot.ItemId == default)
            {
                availableQuantity += maximumStack;
                compatibleSlots.Add(slotIndex);
            }
            else if (itemType == ItemType.Equipment)
            {
                // Equipment slots are not valid target slots because they can have randomized stats
                continue;
            }
            else if (slot.ItemId == itemDescriptorId)
            {
                var availableQuantityInSlot = maximumStack - slot.Quantity;
                if (availableQuantityInSlot <= 0)
                {
                    continue;
                }

                availableQuantity += availableQuantityInSlot;
                compatibleSlots.Add(slotIndex);
            }
        }

        return compatibleSlots.ToArray();
    }

    public static int FindQuantityOfItem<TItem>(Guid itemDescriptorId, TItem?[] slots) where TItem : Item
    {
        return slots.Where(slot => slot?.ItemId == itemDescriptorId)
            .Aggregate(0, (totalQuantity, slot) => totalQuantity + slot.Quantity);
    }

    public static int FindSpaceForItem<TItem>(
        Guid itemDescriptorId,
        ItemType itemType,
        int maximumStack,
        int slotHint,
        int searchQuantity,
        TItem?[] slots
    ) where TItem : Item
    {
        Debug.Assert(itemDescriptorId != default);
        Debug.Assert(slots != default);

        var availableQuantity = 0;

        for (var slotIndex = 0; availableQuantity < searchQuantity && slotIndex < slots.Length; ++slotIndex)
        {
            var slot = slots[(slotIndex + Math.Max(0, slotHint)) % slots.Length];
            if (slot == null || slot.ItemId == default)
            {
                availableQuantity += maximumStack;
            }
            else if (itemType == ItemType.Equipment)
            {
                // Equipment slots are not valid target slots because they can have randomized stats
                continue;
            }
            else if (slot.ItemId == itemDescriptorId)
            {
                availableQuantity += maximumStack - slot.Quantity;
            }
        }

        return Math.Min(availableQuantity, searchQuantity);
    }

    public virtual void Set(Item item)
    {
        ItemId = item.ItemId;
        Quantity = item.Quantity;
        BagId = item.BagId;
        Bag = item.Bag;
        Properties = new ItemProperties(item.Properties);
    }

    /// <summary>
    ///     Try to get the bag, with an additional attempt to load it if it is not already loaded (it should be if this is even
    ///     a bag item).
    /// </summary>
    /// <param name="bag">the bag if there is one associated with this <see cref="Item" /></param>
    /// <returns>if <paramref name="bag" /> is not <see langword="null" /></returns>
    public bool TryGetBag(out Bag bag)
    {
        bag = Bag;

        if (bag == null)
        {
            var descriptor = Descriptor;
            if (descriptor?.ItemType == ItemType.Bag)
            {
                if (!Bag.TryGetBag(BagId ?? default, out bag))
                {
                    return false;
                }

                Bag = bag;
                return true;
            }

            return false;
        }

        // Remove any items from this bag that have been removed from the game
        foreach (var slot in bag.Slots)
        {
            if (ItemDescriptor.TryGet(slot.ItemId, out _))
            {
                continue;
            }

            slot.Set(None);
        }

        return true;
    }

    public void ApplyEnchantment(int newLevel)
    {
        if (Descriptor?.ItemType != ItemType.Equipment || Properties == null)
        {
            return;
        }

        newLevel = Math.Clamp(newLevel, 0, 8);
        int currentLevel = Properties.EnchantmentLevel;

        if (newLevel == currentLevel)
        {
            return;
        }

        if (newLevel > currentLevel)
        {
            double factor = 0.01;
            int GetEnchantmentEffectBonus(ItemEffect effectType)
            {
                var total = 0;
                if (Properties.EnchantmentEffectRolls != null)
                {
                    foreach (var levelBonuses in Properties.EnchantmentEffectRolls.Values)
                    {
                        if (levelBonuses.TryGetValue(effectType, out var bonus))
                        {
                            total += bonus;
                        }
                    }
                }

                return total;
            }

            var baseEffects = new Dictionary<ItemEffect, int>();

            if (Descriptor.Effects?.Count > 0)
            {
                foreach (var effect in Descriptor.Effects)
                {
                    baseEffects[effect.Type] = effect.Percentage;
                }
            }

            if (Properties.EffectModifiers?.Count > 0)
            {
                foreach (var (effectType, effectData) in Properties.EffectModifiers)
                {
                    if (baseEffects.ContainsKey(effectType))
                    {
                        continue;
                    }

                    var recordedBonus = GetEnchantmentEffectBonus(effectType);
                    var baseValue = Math.Max(0, effectData.Percentage - recordedBonus);
                    if (baseValue > 0)
                    {
                        baseEffects[effectType] = baseValue;
                    }
                }
            }

            for (int lvl = currentLevel + 1; lvl <= newLevel; lvl++)
            {
                int[] statBonuses = new int[Enum.GetValues(typeof(Stat)).Length];
                int[] vitalBonuses = new int[Enum.GetValues(typeof(Vital)).Length];
                Dictionary<ItemEffect, int> effectBonuses = new();
                var baseDamageBonus = 0;

                foreach (Stat stat in Enum.GetValues(typeof(Stat)))
                {
                    var statIndex = (int)stat;
                    int baseStat = Descriptor.StatsGiven[statIndex];

                    double levelInfluence = Math.Log2(lvl + 1) + Math.Sqrt(lvl);
                    int bonus = (int)Math.Ceiling(baseStat * factor * levelInfluence);

                    Properties.StatModifiers[statIndex] += bonus;
                    statBonuses[statIndex] = bonus;
                }

                foreach (Vital vital in Enum.GetValues(typeof(Vital)))
                {
                    var vitalIndex = (int)vital;
                    int baseVital = (int)Descriptor.VitalsGiven[vitalIndex];

                    double levelInfluence = Math.Log2(lvl + 1) + Math.Sqrt(lvl);
                    int bonus = (int)Math.Ceiling(baseVital * factor * levelInfluence);

                    Properties.VitalModifiers[vitalIndex] += bonus;
                    vitalBonuses[vitalIndex] = bonus;
                }

                if (baseEffects.Count > 0)
                {
                    Properties.EffectModifiers ??= new Dictionary<ItemEffect, EffectData>();

                    foreach (var (effectType, baseValue) in baseEffects)
                    {
                        var levelInfluence = Math.Log2(lvl + 1) + Math.Sqrt(lvl);
                        var bonus = (int)Math.Ceiling(baseValue * factor * levelInfluence);
                        if (bonus == 0)
                        {
                            continue;
                        }

                        Properties.EffectModifiers.TryGetValue(effectType, out var existingEffect);
                        existingEffect ??= new EffectData(effectType, 0);
                        existingEffect.Percentage += bonus;
                        existingEffect.IsPassive = true;
                        Properties.EffectModifiers[effectType] = existingEffect;
                        effectBonuses[effectType] = bonus;
                    }
                }

                if (Descriptor.EquipmentSlot == Options.Instance.Equipment.WeaponSlot)
                {
                    var baseDamage = Descriptor.Damage;
                    var levelInfluence = Math.Log2(lvl + 1) + Math.Sqrt(lvl);
                    baseDamageBonus = (int)Math.Ceiling(baseDamage * factor * levelInfluence);
                    Properties.BaseDamageModifier += baseDamageBonus;
                }

                // Combinar stats y vitals en un solo diccionario con índices consecutivos si quieres,
                // o guardar por separado si prefieres. Aquí los unimos en uno solo:
                Properties.EnchantmentRolls[lvl] =
                    statBonuses
                        .Concat(vitalBonuses)
                        .Concat(new[] { baseDamageBonus })
                        .ToArray();

                if (effectBonuses.Count > 0)
                {
                    Properties.EnchantmentEffectRolls[lvl] = new Dictionary<ItemEffect, int>(effectBonuses);
                }
            }
        }
        else
        {
            for (int lvl = currentLevel; lvl > newLevel; lvl--)
            {
                if (Properties.EnchantmentRolls.TryGetValue(lvl, out var levelBonuses))
                {
                    int statCount = Enum.GetValues(typeof(Stat)).Length;
                    int vitalCount = Enum.GetValues(typeof(Vital)).Length;

                    for (int i = 0; i < statCount; i++)
                    {
                        int bonus = levelBonuses[i];
                        Properties.StatModifiers[i] -= bonus;
                        Properties.StatModifiers[i] = Math.Max(0, Properties.StatModifiers[i]);
                    }

                    for (int i = 0; i < vitalCount; i++)
                    {
                        int bonus = levelBonuses[statCount + i];
                        Properties.VitalModifiers[i] -= bonus;
                        Properties.VitalModifiers[i] = Math.Max(0, Properties.VitalModifiers[i]);
                    }

                    var damageBonusIndex = statCount + vitalCount;
                    if (levelBonuses.Length > damageBonusIndex)
                    {
                        var bonus = levelBonuses[damageBonusIndex];
                        Properties.BaseDamageModifier -= bonus;
                        Properties.BaseDamageModifier = Math.Max(0, Properties.BaseDamageModifier);
                    }

                    Properties.EnchantmentRolls.Remove(lvl);
                }

                if (Properties.EnchantmentEffectRolls.TryGetValue(lvl, out var effectBonuses))
                {
                    if (Properties.EffectModifiers != null)
                    {
                        foreach (var (effectType, bonus) in effectBonuses)
                        {
                            if (!Properties.EffectModifiers.TryGetValue(effectType, out var effectData))
                            {
                                continue;
                            }

                            effectData.Percentage = Math.Max(0, effectData.Percentage - bonus);

                            if (effectData.Percentage <= 0)
                            {
                                Properties.EffectModifiers.Remove(effectType);
                            }
                            else
                            {
                                Properties.EffectModifiers[effectType] = effectData;
                            }
                        }
                    }

                    Properties.EnchantmentEffectRolls.Remove(lvl);
                }
            }
        }

        Properties.EnchantmentLevel = newLevel;
    }
    public bool ApplyRuneUpgrade(Item equipment, Item runeItem, out bool success, out string resultMessage)
    {
        success = false;
        resultMessage = "";

        // 1) Validaciones básicas
        if (Descriptor?.ItemType != ItemType.Equipment || Properties == null)
        {
            resultMessage = "El ítem no es un equipamiento válido.";
            return false;
        }
        if (runeItem?.Descriptor == null
         || runeItem.Descriptor.ItemType != ItemType.Resource
         || runeItem.Descriptor.Subtype != "Rune")
        {
            resultMessage = "El ítem usado no es una Runa válida.";
            return false;
        }

        // 2) ¿A qué apunta la runa y cuánto modifica?
        var desc = runeItem.Descriptor;
        int targetStat = (int)desc.TargetStat;
        int targetVit = (int)desc.TargetVital;
        var targetEffect = desc.TargetEffect;
        var amount = desc.AmountModifier;

        if (amount == 0)
        {
            resultMessage = "Esta Runa no tiene un modificador válido.";
            return false;
        }

        bool isStat = targetStat >= 0 && targetStat < Properties.StatModifiers.Length;
        bool isVital = targetVit >= 0 && targetVit < Properties.VitalModifiers.Length;
        bool isEffect = targetEffect != ItemEffect.None;

        var validTargets = 0;
        validTargets += isStat ? 1 : 0;
        validTargets += isVital ? 1 : 0;
        validTargets += isEffect ? 1 : 0;

        if (validTargets == 0)
        {
            resultMessage = "La Runa no apunta a un destino válido.";
            return false;
        }

        if (validTargets > 1)
        {
            resultMessage = "La Runa no puede modificar más de un destino a la vez.";
            return false;
        }

        // 4) Guardar valor actual para posibles penalizaciones
        int idx = isStat
            ? (int)targetStat
            : (int)targetVit;
        int currentValue = isStat
            ? Properties.StatModifiers[idx]
            : isVital
                ? Properties.VitalModifiers[idx]
                : Properties.EffectModifiers?.GetValueOrDefault(targetEffect)?.Percentage ?? 0;

        // 5) Calcular tasa de éxito sólo en función de MageSink
        var sinkMod = RarityMageoSettings.GetSinkMod(Descriptor.Rarity);
        double sinkFac = Math.Min(0.35, Properties.MageSink / 100.0);
        double finalRate = Math.Clamp(0.35 + sinkFac, 0.05, 0.95);

        // 6) Tirada de éxito
        if (Random.Shared.NextDouble() <= finalRate)
        {
            // 6a) Crítico
            bool isCritical = false;
            double critChance = 0;
            if (finalRate >= 0.9) critChance += 0.15;
            if (Properties.MageSink >= 100) critChance += 0.10;
            if (Random.Shared.NextDouble() <= critChance)
            {
                isCritical = true;
                amount *= 2;
            }

            // 6b) Aplicar bonus
            if (isStat)
            {
                Properties.StatModifiers[idx] += amount;
            }
            else if (isVital)
            {
                Properties.VitalModifiers[idx] += amount;
            }
            else if (isEffect)
            {
                Properties.EffectModifiers ??= new Dictionary<ItemEffect, EffectData>();
                if (!Properties.EffectModifiers.TryGetValue(targetEffect, out var effectData))
                {
                    effectData = new EffectData(targetEffect, 0);
                }
                effectData.Percentage += amount;
                effectData.IsPassive = true;
                effectData.Stacking = EffectStacking.Stack;
                Properties.EffectModifiers[targetEffect] = effectData;
            }

            // 6c) Penalización suave si pasamos 2× valor base (30% de chance, -½ amount)
            int baseVal = isStat
                ? equipment.Descriptor.StatsGiven[idx]
                : isVital
                    ? (int)equipment.Descriptor.VitalsGiven[idx]
                    : equipment.Descriptor.GetEffect(targetEffect)?.Percentage ?? 0;
            int newValue = isStat
                ? Properties.StatModifiers[idx]
                : isVital
                    ? Properties.VitalModifiers[idx]
                    : Properties.EffectModifiers!.GetValueOrDefault(targetEffect)?.Percentage ?? 0;
            if (newValue > baseVal * 2 && Random.Shared.NextDouble() < 0.30)
            {
                int penal = Math.Max(1, amount / 2);
                PenalizeOtherRandomAttribute(isStat, penal, isEffect ? targetEffect : null);
            }

            // 6d) Reducir MageSink
            Properties.MageSink = Math.Max(
                0,
                Properties.MageSink - (int)(amount * 5 * sinkMod)
            );

            // 6e) Mensaje de éxito
            success = true;
            var name = isStat
                ? targetStat.ToString()
                : isVital
                    ? targetVit.ToString()
                    : targetEffect.ToString();
            resultMessage = isCritical
                ? $"🔥 ¡Éxito Crítico! {name} +{amount}."
                : $"¡Éxito! {name} +{amount}.";
        }
        else
        {
            // 7) Fracaso: penalización reducida (-½ amount) y subir MageSink
            int penalty = Math.Min(currentValue, Math.Max(1, amount / 2));
            if (penalty > 0)
            {
                if (isStat)
                {
                    Properties.StatModifiers[idx] -= penalty;
                }
                else if (isVital)
                {
                    Properties.VitalModifiers[idx] -= penalty;
                }
                else if (isEffect)
                {
                    Properties.EffectModifiers ??= new Dictionary<ItemEffect, EffectData>();
                    if (!Properties.EffectModifiers.TryGetValue(targetEffect, out var effectData))
                    {
                        effectData = new EffectData(targetEffect, 0);
                        Properties.EffectModifiers[targetEffect] = effectData;
                    }
                    effectData.Percentage -= penalty;
                    if (effectData.Percentage <= 0)
                    {
                        Properties.EffectModifiers.Remove(targetEffect);
                    }
                }
                var name = isStat
                    ? targetStat.ToString()
                    : isVital
                        ? targetVit.ToString()
                        : targetEffect.ToString();
                resultMessage = $"Falló. {name} -{penalty}.";
            }
            Properties.MageSink += (int)(amount * 10 * sinkMod);
            resultMessage += $" MageSink: {Properties.MageSink}.";
        }

        // 8) Consumir la runa
        runeItem.Quantity--;

        return true;
    }

    private void PenalizeOtherRandomAttribute(bool isStat, int amount, ItemEffect? effectToProtect = null)
    {
        var rng = Random.Shared;
        if (isStat)
        {
            // Elegir un stat al azar que tenga valor >0
            var candidates = Properties.StatModifiers
                .Select((v, i) => (v, i))
                .Where(x => x.v > 0)
                .ToArray();
            if (candidates.Length == 0) return;
            var idx = candidates[rng.Next(candidates.Length)].i;
            int reduce = Math.Min(Properties.StatModifiers[idx], amount);
            Properties.StatModifiers[idx] -= reduce;
        }
        else
        {
            // Igual para vitals o efectos adicionales
            if (effectToProtect == null)
            {
                var candidates = Properties.VitalModifiers
                    .Select((v, i) => (v, i))
                    .Where(x => x.v > 0)
                    .ToArray();
                if (candidates.Length == 0) return;
                var idx = candidates[rng.Next(candidates.Length)].i;
                int reduce = Math.Min(Properties.VitalModifiers[idx], amount);
                Properties.VitalModifiers[idx] -= reduce;
            }
            else
            {
                Properties.EffectModifiers ??= new Dictionary<ItemEffect, EffectData>();
                var candidates = Properties.EffectModifiers
                    .Where(kvp => kvp.Key != effectToProtect && kvp.Value.Percentage > 0)
                    .Select(kvp => kvp.Key)
                    .ToArray();
                if (candidates.Length == 0) return;
                var chosen = candidates[rng.Next(candidates.Length)];
                var effect = Properties.EffectModifiers[chosen];
                var reduce = Math.Min(effect.Percentage, amount);
                effect.Percentage -= reduce;
                if (effect.Percentage <= 0)
                {
                    Properties.EffectModifiers.Remove(chosen);
                }
                else
                {
                    Properties.EffectModifiers[chosen] = effect;
                }
            }
        }
    }

    public static class RarityMageoSettings
    {
        // Multiplicador de sink por rareza
        private static readonly Dictionary<int, double> SinkModTable = new()
    {
        { 0, 1.2 },   // None
        { 1, 1.0 },   // Common
        { 2, 0.9 },   // Uncommon
        { 3, 0.8 },   // Rare
        { 4, 0.7 },   // Epic
        { 5, 0.6 },   // Legendary
    };

        public static double GetSinkMod(int rarity)
            => SinkModTable.TryGetValue(rarity, out var m) ? m : 1.2;
    }


}
