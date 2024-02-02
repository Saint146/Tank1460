using Microsoft.Xna.Framework;

namespace Tank1460.Audio;

internal interface ISoundPlayer
{
    bool IsMuted { get; }

    void Play(Sound sound);

    void Loop(Sound sound);

    void Perform(GameTime gameTime);

    void PauseAndPushState();

    void ResumeAndPopState();

    void Mute();

    void Unmute();
}