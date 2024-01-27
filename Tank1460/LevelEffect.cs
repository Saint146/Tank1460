namespace Tank1460;

public abstract class LevelEffect : Effect
{
    protected Level Level;

    protected LevelEffect(Level level) : base()
    {
        Level = level;
    }
}