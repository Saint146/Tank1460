using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public class TankEffects
{
    private readonly List<TankEffect> _effects = new();

    public delegate void TankEffectEvent(TankEffect effect);

    public event TankEffectEvent EffectAdded;
    public event TankEffectEvent EffectRemoved;

    public bool HasEffect<T>() where T : TankEffect
    {
        return _effects.Any(effect => effect is T);
    }

    public void Add(TankEffect effect)
    {
        _effects.Add(effect);
        EffectAdded?.Invoke(effect);
    }

    public void RemoveAll(Predicate<TankEffect> match)
    {
        var effectsToRemove = _effects.Where(effect => match(effect)).ToList();
        effectsToRemove.ForEach(effect =>
        {
            _effects.Remove(effect);
            EffectRemoved?.Invoke(effect);
        });
    }

    public void AddExclusive(TankEffect effect)
    {
        RemoveAll(e => e.GetType().IsInstanceOfType(effect));
        Add(effect);
    }

    public void RemoveAll<T>() where T : TankEffect
    {
        RemoveAll(e => e is T);
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