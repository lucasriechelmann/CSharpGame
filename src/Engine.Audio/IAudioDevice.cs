namespace Engine.Audio;

/// <summary>
/// What games and systems use to make sound. It is an interface so game code never touches OpenAL (the same layering as input
/// and rendering), and so tests and machines without a sound card can use <see cref="NullAudioDevice"/>.
/// </summary>
public interface IAudioDevice : IDisposable
{
    /// <summary>Human-readable description of the backend, shown in the debug overlay (e.g. the OpenAL device name or why audio is off).</summary>
    string Description { get; }

    /// <summary>True when sounds actually reach a speaker; false for a silent fallback.</summary>
    bool IsAudible { get; }

    /// <summary>Overall volume, 0 (mute) to 1, applied on top of each sound's own volume.</summary>
    float MasterVolume { get; set; }

    /// <summary>Number of voices currently playing.</summary>
    int ActiveVoices { get; }

    /// <summary>
    /// Uploads a clip to the sound card now instead of on its first play, so the first play does not cost a frame hitch.
    /// Call it for the sounds a level will need while loading. Playing an un-preloaded clip still works.
    /// </summary>
    /// <param name="clip">The clip to prepare.</param>
    void Preload(AudioClip clip);

    /// <summary>Starts a sound. When every voice is busy the oldest one is cut off so new sounds are never dropped.</summary>
    /// <param name="clip">What to play.</param>
    /// <param name="volume">This sound's volume, 0 to 1 (or higher to boost).</param>
    /// <param name="pitch">Playback speed and pitch multiplier; 1 is normal, 2 is an octave up.</param>
    /// <param name="loop">Repeat until stopped.</param>
    /// <returns>A handle for <see cref="Stop"/> and <see cref="IsPlaying"/>.</returns>
    VoiceId Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false);

    /// <summary>Stops a sound early. Stale or <see cref="VoiceId.None"/> handles are ignored.</summary>
    void Stop(VoiceId voice);

    /// <summary>True while the handle's sound is still playing.</summary>
    bool IsPlaying(VoiceId voice);

    /// <summary>Stops every sound.</summary>
    void StopAll();

    /// <summary>Call once per frame: frees voices whose sounds have finished and refreshes <see cref="ActiveVoices"/>.</summary>
    void Update();
}
