using System.Collections.Generic;
using Intersect.Config;
using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Client.Localization;

public static class ItemLocalizationHelper
{
    public static string GetItemTypeName(ItemType type, string? fallback = null)
    {
        if (Strings.ItemDescription.ItemTypes.TryGetValue((int)type, out var localizedType))
        {
            return localizedType.ToString();
        }

        return fallback ?? type.ToString();
    }

    public static string GetItemSubtypeName(string? subtype)
    {
        if (string.IsNullOrWhiteSpace(subtype))
        {
            return string.Empty;
        }

        CacheSubtype(subtype);

        return Strings.ItemDescription.ItemSubtypes.TryGetValue(subtype, out var localizedSubtype)
            ? localizedSubtype.ToString()
            : subtype;
    }

    public static string GetItemRarityName(int rarity)
    {
        if (!Options.Instance.Items.TryGetRarityName(rarity, out var rarityName) ||
            string.IsNullOrWhiteSpace(rarityName))
        {
            return string.Empty;
        }

        CacheRarity(rarityName);

        return Strings.ItemDescription.Rarity.TryGetValue(rarityName, out var localizedRarity)
            ? localizedRarity.ToString()
            : rarityName;
    }

    public static void CacheSubtype(string subtype)
    {
        if (string.IsNullOrWhiteSpace(subtype))
        {
            return;
        }

        if (!Strings.ItemDescription.ItemSubtypes.ContainsKey(subtype))
        {
            Strings.ItemDescription.ItemSubtypes[subtype] = subtype;
        }
    }

    public static void CacheSubtypes(IEnumerable<string> subtypes)
    {
        foreach (var subtype in subtypes)
        {
            CacheSubtype(subtype);
        }
    }

    public static void CacheRarity(string rarityName)
    {
        if (string.IsNullOrWhiteSpace(rarityName))
        {
            return;
        }

        if (!Strings.ItemDescription.Rarity.ContainsKey(rarityName))
        {
            Strings.ItemDescription.Rarity[rarityName] = rarityName;
        }
    }
}
