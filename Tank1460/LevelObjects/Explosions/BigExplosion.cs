using Tank1460.Globals;

namespace Tank1460.LevelObjects.Explosions;

public class BigExplosion : Explosion
{
    public BigExplosion(Level level) : base(level)
    {
    }

    protected override string TexturePath() => @"Sprites/Explosions/Big";

    protected override double FrameTime() => GameRules.TimeInFrames(4);
}