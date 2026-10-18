using System.Numerics;

namespace Engine.Audio;

/// <summary>
/// A device that plays nothing. Used when no sound card or OpenAL library is available, so the game still runs; <see cref="Description"/>
/// says why, and the debug overlay shows it, so silence is never a mystery.
/// </summary>
/// <param name="reason">Why audio is off (shown in the overlay).</param>
public sealed class NullAudioDevice(string reason) : IAudioDevice
{
    /// <inheritdoc />
    public string Description { get; } = $"silent ({reason})";

    /// <inheritdoc />
    public bool IsAudible => false;

    /// <inheritdoc />
    public float MasterVolume { get; set; } = 1f;

    /// <inheritdoc />
    public int ActiveVoices => 0;

    /// <inheritdoc />
    public void Preload(AudioClip clip) => ArgumentNullException.ThrowIfNull(clip);

    /// <inheritdoc />
    public VoiceId Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        ArgumentNullException.ThrowIfNull(clip);
        return VoiceId.None;
    }

    /// <inheritdoc />
    public AudioMixer Mixer { get; } = new();

    /// <inheritdoc />
    public VoiceId PlayIn(AudioClip clip, AudioGroup? group, float volume = 1f, float pitch = 1f, bool loop = false) => Play(clip, volume, pitch, loop);

    /// <inheritdoc />
    public VoiceId PlayAt(AudioClip clip, Vector3 position, Spatialization spatial, AudioGroup? group = null, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        spatial.Validate();
        return Play(clip, volume, pitch, loop);
    }

    /// <inheritdoc />
    public VoiceId PlayStream(IAudioStream stream, AudioGroup? group = null, float volume = 1f, bool loop = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.Dispose(); // nothing will read it, and the device owns it
        return VoiceId.None;
    }

    /// <inheritdoc />
    public void SetVolume(VoiceId voice, float volume)
    {
    }

    /// <inheritdoc />
    public void SetPosition(VoiceId voice, Vector3 position)
    {
    }

    /// <inheritdoc />
    public void SetListener(Vector3 position, Vector3 forward, Vector3 up)
    {
    }

    /// <inheritdoc />
    public void Stop(VoiceId voice)
    {
    }

    /// <inheritdoc />
    public bool IsPlaying(VoiceId voice) => false;

    /// <inheritdoc />
    public void StopAll()
    {
    }

    /// <inheritdoc />
    public void Update()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
