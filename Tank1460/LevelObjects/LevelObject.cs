using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Tank1460.Common.Extensions;
using Tank1460.Extensions;
using Tank1460.Globals;

namespace Tank1460.LevelObjects;

public abstract class LevelObject : DrawableObject
{
    public Point Position
    {
        get => _position;
        set
        {
            _position = value;
            CalculateBoundingRectangle();
        }
    }

    public Rectangle BoundingRectangle { get; private set; }

    public Rectangle TileRectangle { get; private set; }

    public virtual CollisionType CollisionType => CollisionType.None;

    protected TimedAnimationPlayer Sprite { get; } = new();

    protected abstract IAnimation GetDefaultAnimation();

    protected Rectangle LocalBounds
    {
        get => Sprite.VisibleRect;
        set
        {
            Sprite.VisibleRect = value;
            CalculateBoundingRectangle();
        }
    }

    private Point _position;

    protected LevelObject(Level level) : base(level)
    {
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Sprite.Draw(spriteBatch, Position.ToVector2());

#if DEBUG
        if (GameRules.ShowObjectsBoundaries)
        {
            spriteBatch.DrawRectangle(BoundingRectangle, Color.Red);
            spriteBatch.DrawPoint(Position.X, Position.Y, Color.Magenta);
            foreach (var tile in TileRectangle.GetAllPoints())
                spriteBatch.DrawRectangle(Level.GetTileBounds(tile.X, tile.Y), new Color(0x22222222));
        }
#endif
    }

    public void Spawn(Point position)
    {
        LoadContent();
        Sprite.PlayAnimation(GetDefaultAnimation());

        Position = position;
    }

    public void SpawnViaCenterPosition(Point centerPosition)
    {
        LoadContent();
        Sprite.PlayAnimation(GetDefaultAnimation());

        Position = new Point(centerPosition.X - LocalBounds.Width / 2, centerPosition.Y - LocalBounds.Height / 2);
    }

    protected abstract void LoadContent();

    private void CalculateBoundingRectangle()
    {
        BoundingRectangle = LocalBounds.Add(Position);

        var oldTileRectangle = TileRectangle;
        TileRectangle = BoundingRectangle.RoundToTiles();

        Level.HandleChangeTileBounds(this, oldTileRectangle, TileRectangle);
    }
}