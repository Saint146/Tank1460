using Tank1460.LevelObjects;

namespace Tank1460;

internal abstract class LevelEffect : UpdateableObject
{
    protected LevelEffect(Level level) : base(level)
    {
    }
}