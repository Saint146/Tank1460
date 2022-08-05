using System.Collections.Generic;
using Tank1460.Audio;

namespace Tank1460.Extensions;

public static class SoundChannelExtensions
{
    public static readonly IReadOnlyCollection<SoundChannels> AllSoundChannels = new[]
    {
        SoundChannels.Triangle,
        SoundChannels.Square1,
        SoundChannels.Square2,
        SoundChannels.NoisePcm
    };
}