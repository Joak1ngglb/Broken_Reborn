using Intersect.Framework.Core.GameObjects.Spells;
using Intersect.GameObjects;
namespace Intersect.Server.General;

public static class SpellMath
{
    public static SpellProperties Scale(SpellDescriptor descriptor, SpellProperties? properties = null)
    {
        return descriptor.BuildEffectiveProperties(properties?.Level ?? 1, properties);
    }
}
