using Intersect.Framework.Core.GameObjects.Fishing;
using Intersect.GameObjects.Annotations;
using Intersect.Localization;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public partial class MapFishingSpotAttribute : MapAttribute
{
    public override MapAttributeType Type => MapAttributeType.FishingSpot;

    [EditorLabel("Attributes", "FishingSpotType")]
    [EditorReference(typeof(FishingSpotBase), nameof(FishingSpotBase.Name))]
    public Guid FishingSpotType { get; set; }

    [EditorLabel("Attributes", "Blocked"), EditorBoolean(Style = BooleanStyle.YesNo)]
    public bool IsBlocked { get; set; }

    public override MapAttribute Clone()
    {
        var attribute = (MapFishingSpotAttribute) base.Clone();
        attribute.FishingSpotType = FishingSpotType;
        attribute.IsBlocked = IsBlocked;

        return attribute;
    }
}
