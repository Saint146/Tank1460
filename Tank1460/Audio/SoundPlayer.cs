using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Collections;
using Tank1460.Common.Extensions;
using Tank1460.Extensions;

namespace Tank1460.Audio;

internal class SoundPlayer : ISoundPlayer
{
    public int MinSoundPriority { get; set; } = int.MinValue;

    private readonly Dictionary<Sound, SoundGroup> _sounds = new();
    private readonly Dictionary<SoundChannels, SoundGroup> _toPlay = new();
    private Dictionary<SoundChannels, SoundGroup> _wasPlaying = new();

    private readonly Stack<Dictionary<SoundChannels, SoundGroup>> _states = new();

    public SoundPlayer(ContentManagerEx content)
    {
        LoadContent(content);
        CheckAllSoundsAreLoaded();
    }

    public void Play(Sound sound)
    {
        var targetSound = _sounds[sound];
        Debug.Assert(!targetSound.IsLooped);

        if (targetSound.Priority < MinSoundPriority)
            return;

        // Если хотя бы на одном канале пересекаемся со звуком такого или выше приоритета (но не таким же), ничего не делаем.
        var playingOnTheSameChannels = targetSound.Channels.Select(channel => _toPlay.GetValueOrDefault(channel));
        if (playingOnTheSameChannels.Any(s => s != targetSound && s?.Priority >= targetSound.Priority))
            return;

        // Записываем звук в заявку на этот такт.
        targetSound.Channels.ForEach(channel => _toPlay[channel] = targetSound);
    }

    public void Loop(Sound sound)
    {
        var targetSound = _sounds[sound];
        Debug.Assert(targetSound.IsLooped);

        if (targetSound.Priority < MinSoundPriority)
            return;

        // Если хотя бы на одном канале пересекаемся со звуком такого или выше приоритета или этим же, ничего не делаем.
        var playingOnTheSameChannels = targetSound.Channels.Select(channel => _toPlay.GetValueOrDefault(channel));
        if (playingOnTheSameChannels.Any(s => s?.Priority >= targetSound.Priority))
            return;

        // Записываем луп в заявку на этот такт.
        targetSound.Channels.ForEach(channel => _toPlay[channel] = targetSound);
    }

    public bool IsPlaying(Sound sound)
    {
        var targetSound = _sounds[sound];
        return _wasPlaying.ContainsValue(targetSound);
    }

    public void StopAll()
    {
        foreach (var sound in _wasPlaying.Values.Where(sound => sound.IsPlaying).ToHashSet())
            sound.Stop();

        _wasPlaying.Clear();
    }

    // Каждый такт сначала получаем от всех команды по звукам (Play/Loop/Stop), сортируем по каналам и приоритетам,
    // а затем уже тут непосредственно управляем звучанием.
    public void Perform(GameTime gameTime)
    {
        // TODO Хоспаде, что-то я совсем запутался. Это наконец-то работает как надо, но щас пять утра. Как-нибудь отрефакторить тут все нахер.
        // Наверное, надо разделить звуки и лупы на подклассы и полиморфизм заюзать. Хотя сейчас мне нравится, какие SoundGroup простые и замкнутые.

        // Звуки, звучащие в этом такте. Ниже это запишется в _wasPlaying.
        Dictionary<SoundChannels, SoundGroup> isPlaying = new();

        // Перебираем все каналы.
        foreach (var channel in SoundChannelExtensions.AllSoundChannels)
        {
            var oldSound = _wasPlaying.GetValueOrDefault(channel);

            if (_toPlay.TryGetValue(channel, out var sound))
            {
                // Есть заявка на звук, но сначала надо сравнить со старым с этого же канала.
                if (oldSound is not null && oldSound.IsPlaying)
                {
                    if (!oldSound.IsLooped && oldSound.Priority > sound.Priority)
                    {
                        // Если старый звук приоритетнее и всё ещё звучит, оставляем его и ничего не трогаем.
                        isPlaying[channel] = oldSound;
                        continue;
                    }

                    // Если старый звук не приоритетнее, останавливаем его.
                    if (oldSound != sound)
                        oldSound.Stop();
                }

                // Если это луп, то запускаем, только если он не звучал, иначе продолжаем слушать.
                // Если это обычный звук, [пере]запускаем его.
                if (sound.IsLooped)
                {
                    if (!sound.IsPlaying)
                        sound.Loop();
                }
                else
                {
                    if (!isPlaying.ContainsValue(sound))
                        sound.Play();
                }

                isPlaying[channel] = sound;
            }
            else
            {
                // Заявки на звук не было, проверяем, что звучало ранее.
                // Если это луп, останавливаем и забываем в любом случае.
                // Если это обычный звук и он продолжает звучать, оставляем его и запоминаем. В ином случае забываем про него.
                if (oldSound is null)
                    continue;

                if (oldSound.IsLooped)
                {
                    oldSound.Stop();
                }
                else if (oldSound.IsPlaying)
                    isPlaying[channel] = oldSound;
            }
        }

        // Подготавливаемся к следующему такту.
        _wasPlaying = isPlaying;
        _toPlay.Clear();
    }

    public void PauseAndPushState()
    {
        foreach (var sound in _wasPlaying.Values.Where(sound => sound.IsPlaying).ToHashSet())
            sound.Pause();

        _states.Push(_wasPlaying.ShallowClone());
        _wasPlaying.Clear();
    }

    public void ResumeAndPopState()
    {
        _wasPlaying = _states.Pop();

        // TODO: Если один из звуков использовался между Push и Pop, будет косяк.
         foreach (var sound in _wasPlaying.Values.ToHashSet())
            sound.Resume();
    }

    public void Mute()
    {
        MinSoundPriority = int.MaxValue;
    }

    public void Unmute()
    {
        MinSoundPriority = int.MinValue;
    }

    public void MuteAllWithLessPriorityThan(Sound sound)
    {
        MinSoundPriority = _sounds[sound].Priority;
    }

    private void LoadContent(ContentManagerEx content)
    {
        LoadSound(content, Sound.HitDestroy, SoundChannels.Triangle, 3);

        LoadSound(content, Sound.Shot, SoundChannels.Square1, 2);

        LoadSound(content, Sound.MoveBot, SoundChannels.Square2, 0, true);
        LoadSound(content, Sound.MovePlayer, SoundChannels.Square2, 1, true);
        LoadSound(content, Sound.Slide, SoundChannels.Square2, 2);
        LoadSound(content, Sound.HitHurt, SoundChannels.Square2, 3);
        LoadSound(content, Sound.HitDull, SoundChannels.Square2, 4);
        LoadSound(content, Sound.BonusSpawn, SoundChannels.Square2, 4);
        LoadSound(content, Sound.BonusPickup, SoundChannels.Square2, 5);
        LoadSound(content, Sound.Fail, SoundChannels.Square2, 6);
        LoadSound(content, Sound.Pause, SoundChannels.Square2, 8);

        LoadSound(content, Sound.OneUp, SoundChannels.Square1 | SoundChannels.Square2, 5);

        LoadSound(content, Sound.Highscore, SoundChannels.ThreeMelodic, 5);
        LoadSound(content, Sound.Tick, SoundChannels.ThreeMelodic, 6);
        LoadSound(content, Sound.Reward, SoundChannels.ThreeMelodic, 7);
        LoadSound(content, Sound.Intro, SoundChannels.ThreeMelodic, 8);
        LoadSound(content, Sound.GameOver, SoundChannels.ThreeMelodic, 9);

        LoadSound(content, Sound.ExplosionSmall, SoundChannels.NoisePcm, 5);
        LoadSound(content, Sound.ExplosionBig, SoundChannels.NoisePcm, 8);
    }

    private void LoadSound(ContentManagerEx content, Sound sound, SoundChannels channel, int priority, bool isLooped = false)
    {
        var soundGroup = new SoundGroup(content, sound, channel, priority, isLooped);
        _sounds[sound] = soundGroup;
    }

    private void CheckAllSoundsAreLoaded()
    {
        var allSounds = Enum.GetValues<Sound>();

        foreach (var sound in allSounds)
            Debug.Assert(_sounds.ContainsKey(sound));
    }
}