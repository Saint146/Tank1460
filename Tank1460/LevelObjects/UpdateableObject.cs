using Microsoft.Xna.Framework;

namespace Tank1460.LevelObjects;

public abstract class UpdateableObject
{
    public bool ToRemove { get; private set; }

    protected Level Level { get; private set; }

    protected UpdateableObject(Level level)
    {
        Level = level;
        ToRemove = false;
    }

    public abstract void Update(GameTime gameTime);

    protected internal void Remove()
    {
        if (ToRemove)
            return;

        ToRemove = true;
        Level.HandleObjectRemoved(this);
        Level = null;
    }
}