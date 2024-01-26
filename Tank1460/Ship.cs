using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460;

public class Ship : TankEffect
{
    private IAnimation _animation;
    private readonly TankColor _color;
    private readonly AnimationPlayer _animationPlayer = new();

    public Ship(Level level, TankColor color)
    {
        _color = color;
        LoadContent(level.Content);
    }

    private void LoadContent(ContentManagerEx content)
    {
        var texture = content.LoadRecoloredTexture(@"Sprites/Effects/Ship", $"Sprites/_R/Tank/{_color}");
        _animation = new Animation(texture, true);
        _animationPlayer.PlayAnimation(_animation);
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        _animationPlayer.Draw(spriteBatch, position);
    }
}