using System;

namespace Tank1460.Audio;

[Flags]
public enum SoundChannels
{
    Triangle = 1,
    Square1 = 2,
    Square2 = 4,
    NoisePcm = 8,

    ThreeMelodic = Triangle | Square1 | Square2,
    All = Triangle | Square1 | Square2 | NoisePcm
}