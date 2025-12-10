using System.Collections.Generic;

namespace Intersect.Framework.Core.GameObjects.Items;

public static class EffectExtensions
{
    public static void ApplyEffect(this IDictionary<ItemEffect, int> dest, EffectData effect)
    {
        if (!effect.IsPassive)
        {
            return;
        }

        var value = effect.GetValue();
        switch (effect.Stacking)
        {
            case EffectStacking.Ignore:
                if (!dest.ContainsKey(effect.Type))
                {
                    dest[effect.Type] = value;
                }

                break;
            case EffectStacking.Renew:
                dest[effect.Type] = value;

                break;
            default:
                if (dest.ContainsKey(effect.Type))
                {
                    dest[effect.Type] += value;
                }
                else
                {
                    dest[effect.Type] = value;
                }

                break;
        }
    }
}
