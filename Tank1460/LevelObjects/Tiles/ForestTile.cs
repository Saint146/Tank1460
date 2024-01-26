using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.LevelObjects.Tiles;

public class ForestTile : DestructibleTile
{
    public ForestTile(Level level) : base(level)
    {
    }

    public override CollisionType CollisionType => CollisionType.Shootable;

    public override TileView TileView => TileView.Foreground;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Forest"), false);

    public override bool HandleShot(Shell shell)
    {
        base.HandleShot(shell);
        if (shell.Properties.HasFlag(ShellProperties.Pruning))
            Reduce(shell.Direction, DefaultHeight);

        return false;
    }
}