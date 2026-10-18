using System.Numerics;

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

    /// <summary>The mixer groups sounds can be played through. <see cref="MasterVolume"/> is separate and applies on top of <c>Mixer.Master</c>.</summary>
    AudioMixer Mixer { get; }

    /// <summary>Starts a sound through a mixer group. A group's volume and mute follow the sound live, not only when it starts.</summary>
    /// <param name="clip">What to play.</param>
    /// <param name="group">The group to mix into; null means the master group.</param>
    /// <param name="volume">This sound's volume, 0 to 1 (or higher to boost).</param>
    /// <param name="pitch">Playback speed and pitch multiplier.</param>
    /// <param name="loop">Repeat until stopped.</param>
    VoiceId PlayIn(AudioClip clip, AudioGroup? group, float volume = 1f, float pitch = 1f, bool loop = false);

    /// <summary>
    /// Starts a sound at a place in the world. Only mono clips are positioned (a stereo clip plays unpositioned, as OpenAL does). Move it
    /// later with <see cref="SetPosition"/> and move the ears with <see cref="SetListener"/>.
    /// </summary>
    /// <param name="clip">What to play (mono for positioning to work).</param>
    /// <param name="position">World position of the sound.</param>
    /// <param name="spatial">How the volume falls off with distance.</param>
    /// <param name="group">The group to mix into; null means the master group.</param>
    /// <param name="volume">This sound's volume.</param>
    /// <param name="pitch">Playback speed and pitch multiplier.</param>
    /// <param name="loop">Repeat until stopped.</param>
    VoiceId PlayAt(AudioClip clip, Vector3 position, Spatialization spatial, AudioGroup? group = null, float volume = 1f, float pitch = 1f, bool loop = false);

    /// <summary>
    /// Starts a stream (music, long ambience) that is read while it plays. The device takes ownership: the stream is disposed when the
    /// sound ends or is stopped. It uses one voice like any other sound.
    /// </summary>
    /// <param name="stream">What to play.</param>
    /// <param name="group">The group to mix into; null means the master group.</param>
    /// <param name="volume">This sound's volume.</param>
    /// <param name="loop">Rewind and repeat at the end.</param>
    VoiceId PlayStream(IAudioStream stream, AudioGroup? group = null, float volume = 1f, bool loop = false);

    /// <summary>Changes a playing sound's own volume (the group's volume still applies on top). Stale handles are ignored.</summary>
    void SetVolume(VoiceId voice, float volume);

    /// <summary>Moves a positioned sound. Ignored for stale handles and for sounds that were not started with <see cref="PlayAt"/>.</summary>
    void SetPosition(VoiceId voice, Vector3 position);

    /// <summary>Places the listener (the "ears", normally the camera or the player). Right is <c>cross(forward, up)</c>, as in the rest of the engine.</summary>
    /// <param name="position">World position.</param>
    /// <param name="forward">The direction the listener faces.</param>
    /// <param name="up">The listener's up direction.</param>
    void SetListener(Vector3 position, Vector3 forward, Vector3 up);

    /// <summary>Stops a sound early. Stale or <see cref="VoiceId.None"/> handles are ignored.</summary>
    void Stop(VoiceId voice);

    /// <summary>True while the handle's sound is still playing.</summary>
    bool IsPlaying(VoiceId voice);

    /// <summary>Stops every sound.</summary>
    void StopAll();

    /// <summary>Call once per frame: frees voices whose sounds have finished and refreshes <see cref="ActiveVoices"/>.</summary>
    void Update();
}
