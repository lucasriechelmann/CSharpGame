namespace Engine.Audio;

/// <summary>
/// A source of 16-bit PCM that is read a piece at a time while it plays, for sounds too long to hold in memory as an
/// <see cref="AudioClip"/> (music, ambience). The device pulls from it on the game thread during <see cref="IAudioDevice.Update"/>.
/// </summary>
public interface IAudioStream : IDisposable
{
    /// <summary>Samples per second per channel.</summary>
    int SampleRate { get; }

    /// <summary>1 for mono, 2 for stereo.</summary>
    int Channels { get; }

    /// <summary>Fills <paramref name="buffer"/> with as many whole frames as are left and returns the number of bytes written; 0 means the end.</summary>
    int Read(Span<byte> buffer);

    /// <summary>Goes back to the start (used to loop).</summary>
    void Rewind();
}
