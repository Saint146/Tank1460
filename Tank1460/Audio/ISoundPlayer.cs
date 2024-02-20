using Microsoft.Xna.Framework;

namespace Tank1460.Audio;

internal interface ISoundPlayer
{
    int MinSoundPriority
    { get; set; }

    void Play(Sound sound);

    void Loop(Sound sound);

    bool IsPlaying(Sound sound);

    void StopAll();

    void Perform(GameTime gameTime);

    void PauseAndPushState();

    void ResumeAndPopState();

    void Mute();

    void Unmute();

    void MuteAllWithLessPriorityThan(Sound sound);
}