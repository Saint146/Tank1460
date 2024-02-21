using Microsoft.Xna.Framework;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.LevelObjects.Tiles;

public abstract class Tile : LevelObject
{
    public const int DefaultWidth = 8;
    public const int DefaultHeight = 8;
    public static readonly Point DefaultSize = new(DefaultWidth, DefaultHeight);

    private IAnimation _animation;

    protected abstract IAnimation GetAnimation();

    /// <summary>
    /// Среагировать на попадание снаряда.
    /// </summary>
    /// <returns>Должен ли снаряд взорваться от попадания.</returns>
    public virtual bool HandleShot(Shell shell) => false;

    public abstract TileType Type { get; }

    protected Tile(Level level) : base(level)
    {
    }

    public override void Update(GameTime gameTime)
    {
    }

    public virtual TileLayer TileLayer => TileLayer.Default;

    protected override IAnimation GetDefaultAnimation() => _animation;

    protected override void LoadContent()
    {
        _animation = GetAnimation();
    }
}