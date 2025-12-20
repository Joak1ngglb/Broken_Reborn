using System.ComponentModel;

namespace Intersect.Config;

/// <summary>
/// Feature flag configuration.
/// </summary>
public partial class FeatureOptions
{
    [DefaultValue(false)]
    public bool NewFishingV2 { get; set; } = false;
}
