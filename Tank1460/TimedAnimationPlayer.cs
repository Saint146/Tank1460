using Microsoft.Xna.Framework;

namespace Tank1460;

public class TimedAnimationPlayer : AnimationPlayer
{
    public void ProcessAnimation(GameTime gameTime)
    {
        Animation.Process(gameTime);
    }
}