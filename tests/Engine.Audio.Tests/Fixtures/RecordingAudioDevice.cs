using System.Numerics;

namespace Engine.Audio.Tests.Fixtures;

/// <summary>A device that plays nothing and writes down what it was asked, so systems that drive a device can be tested without a sound card.</summary>
internal sealed class RecordingAudioDevice : IAudioDevice
{
    private int _next = 1;

    public sealed record Voice(VoiceId Id, AudioClip Clip, AudioGroup? Group, float Volume, bool Loop, bool Positioned, Vector3 Position)
    {
        public float CurrentVolume { get; set; } = Volume;

        public Vector3 CurrentPosition { get; set; } = Position;

        public bool Stopped { get; set; }
    }

    public List<Voice> Voices { get; } = new();

    public Vector3 ListenerPosition { get; private set; }

    public Vector3 ListenerForward { get; private set; }

    public string Description => "recording";

    public bool IsAudible => false;

    public float MasterVolume { get; set; } = 1f;

    public int ActiveVoices => Voices.Count(v => !v.Stopped);

    public AudioMixer Mixer { get; } = new();

    public void Preload(AudioClip clip)
    {
    }

    public VoiceId Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false) => PlayIn(clip, null, volume, pitch, loop);

    public VoiceId PlayIn(AudioClip clip, AudioGroup? group, float volume = 1f, float pitch = 1f, bool loop = false) =>
        Add(new Voice(NewId(), clip, group, volume, loop, false, default));

    public VoiceId PlayAt(AudioClip clip, Vector3 position, Spatialization spatial, AudioGroup? group = null, float volume = 1f, float pitch = 1f, bool loop = false) =>
        Add(new Voice(NewId(), clip, group, volume, loop, true, position));

    public VoiceId PlayStream(IAudioStream stream, AudioGroup? group = null, float volume = 1f, bool loop = false) => VoiceId.None;

    public void SetVolume(VoiceId voice, float volume)
    {
        if (Find(voice) is { } v) v.CurrentVolume = volume;
    }

    public void SetPosition(VoiceId voice, Vector3 position)
    {
        if (Find(voice) is { } v) v.CurrentPosition = position;
    }

    public void SetListener(Vector3 position, Vector3 forward, Vector3 up)
    {
        ListenerPosition = position;
        ListenerForward = forward;
    }

    public void Stop(VoiceId voice)
    {
        if (Find(voice) is { } v) v.Stopped = true;
    }

    public bool IsPlaying(VoiceId voice) => Find(voice) is { Stopped: false };

    public void StopAll()
    {
        foreach (var v in Voices) v.Stopped = true;
    }

    public void Update()
    {
    }

    public void Dispose()
    {
    }

    private VoiceId NewId() => new(_next - 1, _next++);

    private VoiceId Add(Voice voice)
    {
        Voices.Add(voice);
        return voice.Id;
    }

    private Voice? Find(VoiceId id) => Voices.FirstOrDefault(v => v.Id == id);
}
