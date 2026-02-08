using System;

namespace Intersect.Framework.Core.Localization;

public static class QuestFieldKey
{
    public const string Name = LocalizationFields.Name;
    public const string StartDescription = "StartDescription";
    public const string BeforeDescription = "BeforeDescription";
    public const string InProgressDescription = "InProgressDescription";
    public const string EndDescription = "EndDescription";

    public static string TaskDescription(Guid taskId) =>
        $"Task:{taskId}:{LocalizationFields.Description}";
}
