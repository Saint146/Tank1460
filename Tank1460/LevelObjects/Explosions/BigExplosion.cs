namespace Tank1460.LevelObjects.Explosions;

public class BigExplosion : Explosion
{
    public BigExplosion(Level level) : base(level)
    {
    }

    protected override string TexturePath() => @"Sprites/Explosions/Big";

    protected override double FrameTime()
    {
        return 4 * Tank1460Game.OneFrameSpan;
    }
}