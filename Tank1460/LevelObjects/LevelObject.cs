using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Tank1460.Extensions;

namespace Tank1460.LevelObjects;

public abstract class LevelObject
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

    private Point _position;

    public Rectangle TileRectangle { get; private set; }

    public bool ToRemove { get; private set; } = false;

    protected Level Level { get; private set; }

    protected IAnimation DefaultAnimation { get; private set; }

    protected TimedAnimationPlayer Sprite { get; } = new();

    protected Rectangle LocalBounds
    {
        get => Sprite.VisibleRect;
        set
        {
            Sprite.VisibleRect = value;
            CalculateBoundingRectangle();
        }
    }

    private void CalculateBoundingRectangle()
    {
        BoundingRectangle = LocalBounds.Add(Position);

        var oldTileRectangle = TileRectangle;
        TileRectangle = BoundingRectangle.RoundToTiles();

        Level.HandleChangeTileBounds(this, oldTileRectangle, TileRectangle);
    }

    public virtual CollisionType CollisionType => CollisionType.None;

    public Rectangle BoundingRectangle { get; private set; }

    protected LevelObject(Level level)
    {
        Level = level;
    }

    protected abstract IAnimation GetDefaultAnimation();

    public virtual void Update(GameTime gameTime, KeyboardState keyboardState)
    {
    }

    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Sprite.Draw(spriteBatch, Position.ToVector2());

#if DEBUG
        if (Tank1460Game.ShowObjectsBoundaries)
        {
            spriteBatch.DrawRectangle(BoundingRectangle, Color.Red);
            spriteBatch.DrawPoint(Position.X, Position.Y, Color.Magenta);
            foreach(var tile in TileRectangle.GetAllPoints())
                spriteBatch.DrawRectangle(Level.GetTileBounds(tile.X, tile.Y), new Color(0x22222222));
        }
#endif
    }

    protected abstract void LoadContent();

    public void Spawn(Point position)
    {
        LoadContent();
        DefaultAnimation = GetDefaultAnimation();
        Sprite.PlayAnimation(DefaultAnimation);

        Position = position;
    }

    public void SpawnViaCenterPosition(Point centerPosition)
    {
        LoadContent();
        DefaultAnimation = GetDefaultAnimation();
        Sprite.PlayAnimation(DefaultAnimation);

        Position = new Point(centerPosition.X - LocalBounds.Width / 2, centerPosition.Y - LocalBounds.Height / 2);
    }

    protected void Remove()
    {
        if (ToRemove)
            return;

        ToRemove = true;
        Level.HandleObjectRemoved(this);
        Level = null;
    }
}