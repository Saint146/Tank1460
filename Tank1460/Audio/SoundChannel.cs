using System;

namespace Tank1460.Audio;

[Flags]
public enum SoundChannels
{
    Triangle = 1 << 0,
    Square1 = 1 << 1,
    Square2 = 1 << 2,
    NoisePcm = 1 << 3,

    ThreeMelodic = Triangle | Square1 | Square2,
    All = Triangle | Square1 | Square2 | NoisePcm
}