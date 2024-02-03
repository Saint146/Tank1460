using Microsoft.Xna.Framework.Graphics;
using Tank1460.Audio;
using Tank1460.Common.Level.Object.Tile;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460.LevelObjects.Tiles;

public class BrickTile : DestructibleTile
{
    public BrickTile(Level level) : base(level)
    {
    }

    public override CollisionType CollisionType => CollisionType.ShootableAndImpassable;

    public override TileType Type => TileType.Brick;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Brick"), false);

    public override bool HandleShot(Shell shell)
    {
        base.HandleShot(shell);

        var isArmorPiercing = shell.Properties.HasFlag(ShellProperties.ArmorPiercing);
        if (shell.ShotBy is PlayerTank || isArmorPiercing)
            Level.SoundPlayer.Play(Sound.HitDestroy);

        Reduce(shell.Direction, DefaultHeight / (isArmorPiercing ? 1 : 2));

        return true;
    }
}