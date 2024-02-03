using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public abstract class Effect
{
    public bool ToRemove { get; private set; } = false;

    public void Remove()
    {
        ToRemove = true;
    }

    public abstract void Update(GameTime gameTime);
}