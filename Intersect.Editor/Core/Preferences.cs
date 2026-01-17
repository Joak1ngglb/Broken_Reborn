using Intersect.Configuration;
using System;
using Microsoft.Win32;

namespace Intersect.Editor.Core;


public static partial class Preferences
{
    private static bool? _enableCursorSprites;
    private static string? _language;

    public static bool EnableCursorSprites
    {
        get => _enableCursorSprites ??= LoadPreferenceBool(nameof(EnableCursorSprites)) ?? false;
        set
        {
            if (_enableCursorSprites == value)
            {
                return;
            }

            _enableCursorSprites = value;
            SavePreference(nameof(EnableCursorSprites), _enableCursorSprites.ToString() ?? string.Empty);
        }
    }

    public static string Language
    {
        get => _language ??= NormalizeLanguage(LoadPreference(nameof(Language))) ?? "en";
        set
        {
            var normalized = NormalizeLanguage(value) ?? "en";
            if (string.Equals(_language, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _language = normalized;
            SavePreference(nameof(Language), _language);
        }
    }

    public static void SavePreference(string key, string value)
    {
        var regkey = Registry.CurrentUser.OpenSubKey("Software", true);

        regkey = regkey.CreateSubKey("IntersectEditor");
        regkey = regkey.CreateSubKey($"{ClientConfiguration.Instance.Host}:{ClientConfiguration.Instance.Port}");

        regkey.SetValue(key, value);
    }

    private static bool? LoadPreferenceBool(string key)
    {
        var rawPreference = LoadPreference(key);
        if (string.IsNullOrWhiteSpace(rawPreference))
        {
            return null;
        }

        return Convert.ToBoolean(rawPreference);
    }

    public static string LoadPreference(string key)
    {
        var regkey = Registry.CurrentUser.OpenSubKey("Software", false);
        regkey = regkey.OpenSubKey("IntersectEditor", false);
        if (regkey == null)
        {
            return string.Empty;
        }

        regkey = regkey.OpenSubKey($"{ClientConfiguration.Instance.Host}:{ClientConfiguration.Instance.Port}");
        if (regkey == null)
        {
            return string.Empty;
        }

        var value = (string) regkey.GetValue(key);
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value;
    }

    private static string? NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language)
            ? null
            : language.Trim().ToLowerInvariant();

}
