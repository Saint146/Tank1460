using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public class TankEffects
{
    private readonly List<TankEffect> _effects = new();

    public bool HasEffect<T>() where T : TankEffect
    {
        return _effects.Any(effect => effect is T);
    }

    public void AddExclusive(TankEffect effect)
    {
        _effects.RemoveAll(e => e.GetType().IsInstanceOfType(effect));
        _effects.Add(effect);
    }

    public void RemoveAll<T>() where T : TankEffect
    {
        _effects.RemoveAll(e => e is T);
    }
    
    public void Update(GameTime gameTime)
    {
        _effects.ForEach(effect => effect.Update(gameTime));
        _effects.RemoveAll(effect => effect.ToRemove);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        _effects.ForEach(effect => effect.Draw(spriteBatch, position));
    }
}