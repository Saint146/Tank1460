using Microsoft.Xna.Framework.Graphics;
using Tank1460.Audio;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460.LevelObjects.Tiles;

public class ConcreteTile : DestructibleTile
{
    public ConcreteTile(Level level) : base(level)
    {
    }
    public override TileType Type => TileType.Concrete;


    public override CollisionType CollisionType => CollisionType.ShootableAndImpassable;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Concrete"), false);

    public override bool HandleShot(Shell shell)
    {
        base.HandleShot(shell);
        if (shell.Properties.HasFlag(ShellProperties.ArmorPiercing))
        {
            Level.SoundPlayer.Play(Sound.HitDestroy);
            Reduce(shell.Direction, DefaultHeight);
        }
        else if (shell.ShotBy is PlayerTank)
        {
            Level.SoundPlayer.Play(Sound.HitDull);
        }

        return true;
    }
}