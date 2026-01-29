using System;
using System.Text;

namespace Intersect.Editor.Localization;

public sealed class TranslationEntryKey
{
    public TranslationEntryKey(string entityType, string entityId, string field, string subPath)
    {
        EntityType = entityType;
        EntityId = entityId;
        Field = field;
        SubPath = subPath;
    }

    public string EntityType { get; set; }

    public string EntityId { get; set; }

    public string Field { get; set; }

    public string SubPath { get; set; }

    public string ToSerializedString()
    {
        return TranslationEntryKeySerializer.Serialize(this);
    }

    public static TranslationEntryKey FromSerializedString(string serialized)
    {
        return TranslationEntryKeySerializer.Deserialize(serialized);
    }

    public static bool TryParseSerialized(string serialized, out TranslationEntryKey? key)
    {
        return TranslationEntryKeySerializer.TryDeserialize(serialized, out key);
    }
}

public static class TranslationEntryKeySerializer
{
    private const string VersionPrefix = "v1";
    private const char Separator = '.';

    public static string Serialize(TranslationEntryKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return string.Join(
            Separator,
            VersionPrefix,
            Encode(key.EntityType),
            Encode(key.EntityId),
            Encode(key.Field),
            Encode(key.SubPath)
        );
    }

    public static TranslationEntryKey Deserialize(string serialized)
    {
        if (TryDeserialize(serialized, out var key) && key != null)
        {
            return key;
        }

        throw new FormatException("Invalid TranslationEntryKey serialization format.");
    }

    public static bool TryDeserialize(string serialized, out TranslationEntryKey? key)
    {
        key = null;
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return false;
        }

        var parts = serialized.Split(Separator);
        if (parts.Length != 5 || !string.Equals(parts[0], VersionPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var entityType = Decode(parts[1]);
            var entityId = Decode(parts[2]);
            var field = Decode(parts[3]);
            var subPath = Decode(parts[4]);
            key = new TranslationEntryKey(entityType, entityId, field, subPath);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }

    private static string Decode(string value)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
    }
}
