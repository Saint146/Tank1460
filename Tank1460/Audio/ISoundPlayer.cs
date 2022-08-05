using Microsoft.Xna.Framework;

namespace Tank1460.Audio;

internal interface ISoundPlayer
{
    void Play(Sound sound);
    void Loop(Sound sound);
    void Perform(GameTime gameTime);
    void PauseAndPushState();
    void ResumeAndPopState();
}