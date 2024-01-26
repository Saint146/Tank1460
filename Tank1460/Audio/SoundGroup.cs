using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;
using System.Linq;
using Tank1460.Extensions;

namespace Tank1460.Audio;

internal class SoundGroup
{
    public Sound Sound { get; }

    // Флаг используется при инициализации звуков для удобства, а уже здесь для быстроты он раскидывается на список.
    public List<SoundChannels> Channels { get; }

    /// <summary>
    /// Больше число — выше приоритет.
    /// </summary>
    public int Priority { get; }

    public bool IsLooped { get; }
    public bool IsPlaying => _lastPlayed?.State == SoundState.Playing;

    private readonly List<SoundEffectInstance> _soundEffects;
    private SoundEffectInstance _lastPlayed;

    public SoundGroup(ContentManagerEx content, Sound sound, SoundChannels channels, int priority, bool isLooped = false)
    {
        Sound = sound;
        Channels = SoundChannelExtensions.AllSoundChannels
            .Where(value => (value & channels) != 0)
            .ToList();
        Priority = priority;
        IsLooped = isLooped;

        // Загружаем все вариации звука из подпапки.
        _soundEffects = content.MassLoadContent<SoundEffect>($"Sounds/8bit/{sound}")
            .Values
            .Select(soundEffect => soundEffect.CreateInstance())
            .ToList();

        foreach (var soundEffect in _soundEffects)
            soundEffect.IsLooped = IsLooped;
    }

    public void Play()
    {
        Debug.Assert(!IsLooped);
        PlayRandom();
    }

    public void Loop()
    {
        Debug.Assert(IsLooped);
        PlayRandom();
    }

    public void Stop()
    {
        _lastPlayed?.Stop();
    }

    private void PlayRandom()
    {
        Stop();
        _lastPlayed = _soundEffects.GetRandom();
        _lastPlayed.Play();
    }

    public void Pause()
    {
        _lastPlayed?.Pause();
    }

    public void Resume()
    {
        _lastPlayed?.Resume();
    }
}