using Microsoft.Xna.Framework.Graphics;
using Tank1460.Audio;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460.LevelObjects.Tiles;

public class BrickTile : DestructibleTile
{
    public BrickTile(Level level) : base(level)
    {
    }

    public override CollisionType CollisionType => CollisionType.ShootableAndImpassable;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Brick"), false);

    public override void HandleShot(Shell shell)
    {
        base.HandleShot(shell);
        if (shell.ShotBy is PlayerTank)
            Level.SoundPlayer.Play(Sound.HitDestroy);

        Reduce(shell.Direction, DefaultHeight / (shell.Properties.HasFlag(ShellProperties.ArmorPiercing) ? 1 : 2));
    }
}