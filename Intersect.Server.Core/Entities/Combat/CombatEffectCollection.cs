using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Server.Entities;

namespace Intersect.Server.Entities.Combat;

public sealed class CombatEffectCollection
{
    private readonly Dictionary<ItemEffect, EffectValue> mEffects = new();

    public static CombatEffectCollection From(IEnumerable<EffectData> effects)
    {
        var collection = new CombatEffectCollection();
        collection.AddRange(effects);

        return collection;
    }

    public EffectValue Get(ItemEffect effect)
    {
        return mEffects.TryGetValue(effect, out var value) ? value : default;
    }

    public void AddRange(IEnumerable<EffectData>? effects)
    {
        if (effects == null)
        {
            return;
        }

        foreach (var effect in effects.Where(effect => !effect.IsPassive))
        {
            Add(effect);
        }
    }

    private void Add(EffectData effect)
    {
        var value = effect.GetValues();
        switch (effect.Stacking)
        {
            case EffectStacking.Ignore:
                if (!mEffects.ContainsKey(effect.Type))
                {
                    mEffects[effect.Type] = value;
                }

                break;
            case EffectStacking.Renew:
                mEffects[effect.Type] = value;

                break;
            default:
                if (mEffects.ContainsKey(effect.Type))
                {
                    mEffects[effect.Type] = mEffects[effect.Type].Add(value);
                }
                else
                {
                    mEffects[effect.Type] = value;
                }

                break;
        }
    }
}

public sealed record CombatantEffects(Entity Entity, CombatEffectCollection ActiveEffects)
{
    public EffectValue GetTotalEffectValue(ItemEffect effect)
    {
        var passive = Entity.GetPassiveEffectValues(effect);
        var active = ActiveEffects.Get(effect);

        return passive.Add(active);
    }
}

public sealed record CombatEffectsSnapshot(CombatantEffects Attacker, CombatantEffects Defender)
{
    public CombatantEffects Get(Entity entity)
    {
        if (entity.Id == Attacker.Entity.Id)
        {
            return Attacker;
        }

        if (entity.Id == Defender.Entity.Id)
        {
            return Defender;
        }

        throw new ArgumentException("Unknown entity in snapshot", nameof(entity));
    }
}
