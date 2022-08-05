namespace Tank1460.LevelObjects.Explosions;

public class CommonExplosion : Explosion
{
    public CommonExplosion(Level level) : base(level)
    {
    }

    protected override string TexturePath() => @"Sprites/Explosions/Common";

    protected override double FrameTime() => 3 * Tank1460Game.OneFrameSpan;
}