using Microsoft.Xna.Framework.Graphics;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.LevelObjects.Tiles;

public class ForestTile : DestructibleTile
{
    public ForestTile(Level level) : base(level)
    {
    }
    public override TileType Type => TileType.Forest;


    public override CollisionType CollisionType => CollisionType.Shootable;

    public override TileLayer TileLayer => TileLayer.Foreground;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Forest"), false);

    public override bool HandleShot(Shell shell)
    {
        base.HandleShot(shell);
        if (shell.Properties.HasFlag(ShellProperties.Pruning))
            Reduce(shell.Direction, DefaultHeight);

        return false;
    }
}