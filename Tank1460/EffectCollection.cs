using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tank1460;

public class EffectCollection<T> where T : Effect
{
    private readonly List<T> _effects = new();

    public delegate void EffectEvent<in TEvent>(TEvent effect);

    public event EffectEvent<T> EffectAdded;
    public event EffectEvent<T> EffectRemoved;

    public bool HasEffect<TEffect>() where TEffect : T
    {
        return _effects.Any(effect => effect is TEffect);
    }

    public void Add(T effect)
    {
        _effects.Add(effect);
        EffectAdded?.Invoke(effect);
    }

    public void RemoveAll(Predicate<T> match)
    {
        var effectsToRemove = _effects.Where(effect => match(effect)).ToList();
        effectsToRemove.ForEach(effect =>
        {
            _effects.Remove(effect);
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
        _effects.ForEach(effect => effect.Update(gameTime));
        RemoveAll(effect => effect.ToRemove);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        _effects.ForEach(effect => effect.Draw(spriteBatch, position));
    }
}