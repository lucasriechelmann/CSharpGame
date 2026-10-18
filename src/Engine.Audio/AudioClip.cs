namespace Engine.Audio;

/// <summary>
/// A decoded sound: raw 16-bit PCM samples plus the format needed to play them. It is plain data with no OpenAL objects in it, so
/// it can be decoded, tested and cached without an audio device; the device uploads it to the sound card the first time it plays.
/// </summary>
public sealed class AudioClip
{
    /// <summary>Creates a clip from already-decoded samples.</summary>
    /// <param name="sampleRate">Samples per second per channel (e.g. 44100).</param>
    /// <param name="channels">1 (mono) or 2 (stereo).</param>
    /// <param name="pcm16">Interleaved signed 16-bit little-endian samples; the length must be a whole number of frames.</param>
    /// <exception cref="ArgumentOutOfRangeException">The rate is not positive or the channel count is not 1 or 2.</exception>
    /// <exception cref="ArgumentException">The data is empty or ends mid-frame.</exception>
    public AudioClip(int sampleRate, int channels, byte[] pcm16)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Sample rate must be positive.");
        if (channels is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(channels), channels, "Only mono (1) and stereo (2) clips are supported.");
        if (pcm16.Length == 0) throw new ArgumentException("A clip needs at least one sample.", nameof(pcm16));
        if (pcm16.Length % (channels * sizeof(short)) != 0)
            throw new ArgumentException($"{pcm16.Length} bytes is not a whole number of {channels}-channel 16-bit frames.", nameof(pcm16));

        SampleRate = sampleRate;
        Channels = channels;
        Pcm16 = pcm16;
    }

    /// <summary>Samples per second per channel.</summary>
    public int SampleRate { get; }

    /// <summary>1 for mono, 2 for stereo.</summary>
    public int Channels { get; }

    /// <summary>Interleaved signed 16-bit little-endian samples.</summary>
    public byte[] Pcm16 { get; }

    /// <summary>Number of sample frames (one sample per channel each).</summary>
    public int FrameCount => Pcm16.Length / (Channels * sizeof(short));

    /// <summary>Playing time in seconds at normal pitch.</summary>
    public double Duration => (double)FrameCount / SampleRate;
}
