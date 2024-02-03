using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tank1460;

public class EffectCollection<T> where T : Effect
{
    protected readonly List<T> Effects = new();

    public delegate void EffectEvent<in TEvent>(TEvent effect);

    public event EffectEvent<T> EffectAdded;
    public event EffectEvent<T> EffectRemoved;

    public bool HasEffect<TEffect>() where TEffect : T
    {
        return Effects.Any(effect => effect is TEffect);
    }

    public void Add(T effect)
    {
        Effects.Add(effect);
        EffectAdded?.Invoke(effect);
    }

    public void RemoveAll(Predicate<T> match)
    {
        var effectsToRemove = Effects.Where(effect => match(effect)).ToList();
        effectsToRemove.ForEach(effect =>
        {
            Effects.Remove(effect);
            EffectRemoved?.Invoke(effect);
        });
    }

    public void AddExclusive(T effect)
    {
        RemoveAll(e => e.GetType().IsInstanceOfType(effect));
        Add(effect);
    }

    public void RemoveAll<TEffect>() where TEffect : T
    {
        RemoveAll(e => e is TEffect);
    }

    public void Update(GameTime gameTime)
    {
        Effects.ForEach(effect => effect.Update(gameTime));
        RemoveAll(effect => effect.ToRemove);
    }
}