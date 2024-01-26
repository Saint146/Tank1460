using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public class Immobility : TankEffect
{
    private readonly double _effectTime;
    private double _time;

    public Immobility(double effectTime)
    {
        _effectTime = effectTime;
        _time = 0.0;
    }

    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        if (_time > _effectTime)
            Remove();
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
    }
}