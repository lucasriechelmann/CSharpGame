using System.Numerics;
using Silk.NET.OpenAL;

namespace Engine.Audio;

// Playing, stopping and tearing down voices and buffers.
public sealed unsafe partial class OpenAlAudioDevice
{
    /// <inheritdoc />
    public void Preload(AudioClip clip)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(clip);
        BufferFor(clip);
    }

    /// <inheritdoc />
    public VoiceId Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false) => PlayIn(clip, null, volume, pitch, loop);

    /// <inheritdoc />
    public VoiceId PlayIn(AudioClip clip, AudioGroup? group, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(clip);
        if (pitch <= 0f) throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "Pitch must be positive.");

        var voice = Begin(group, volume, out uint source);
        _al.SetSourceProperty(source, SourceInteger.Buffer, (int)BufferFor(clip));
        _al.SetSourceProperty(source, SourceFloat.Pitch, pitch);
        _al.SetSourceProperty(source, SourceBoolean.Looping, loop);
        MakeUnpositioned(source);
        Check("configuring a source");

        _al.SourcePlay(source);
        Check("starting a source");
        return voice;
    }

    /// <inheritdoc />
    public VoiceId PlayAt(AudioClip clip, Vector3 position, Spatialization spatial, AudioGroup? group = null, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(clip);
        if (pitch <= 0f) throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "Pitch must be positive.");
        spatial.Validate();

        var voice = Begin(group, volume, out uint source);
        _al.SetSourceProperty(source, SourceInteger.Buffer, (int)BufferFor(clip));
        _al.SetSourceProperty(source, SourceFloat.Pitch, pitch);
        _al.SetSourceProperty(source, SourceBoolean.Looping, loop);
        _al.SetSourceProperty(source, SourceBoolean.SourceRelative, false);
        _al.SetSourceProperty(source, SourceFloat.ReferenceDistance, spatial.MinDistance);
        _al.SetSourceProperty(source, SourceFloat.MaxDistance, spatial.MaxDistance);
        _al.SetSourceProperty(source, SourceFloat.RolloffFactor, spatial.Rolloff);
        _al.SetSourceProperty(source, SourceVector3.Position, position.X, position.Y, position.Z);
        Check("configuring a positioned source");

        _al.SourcePlay(source);
        Check("starting a source");
        return voice;
    }

    /// <inheritdoc />
    public void SetVolume(VoiceId voice, float volume)
    {
        if (_pool.IsCurrent(voice)) _baseVolume[voice.Index] = Math.Max(volume, 0f);
    }

    /// <inheritdoc />
    public void SetPosition(VoiceId voice, Vector3 position)
    {
        if (!_pool.IsCurrent(voice)) return;

        _al.SetSourceProperty(_sources[voice.Index], SourceVector3.Position, position.X, position.Y, position.Z);
        Check("moving a source");
    }

    /// <inheritdoc />
    public void SetListener(Vector3 position, Vector3 forward, Vector3 up)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _al.SetListenerProperty(ListenerVector3.Position, position.X, position.Y, position.Z);
        float* orientation = stackalloc float[6] { forward.X, forward.Y, forward.Z, up.X, up.Y, up.Z };
        _al.SetListenerProperty(ListenerFloatArray.Orientation, orientation);
        Check("placing the listener");
    }

    /// <inheritdoc />
    public void Stop(VoiceId voice)
    {
        if (!_pool.IsCurrent(voice)) return;

        FreeVoice(voice.Index);
    }

    /// <inheritdoc />
    public bool IsPlaying(VoiceId voice) =>
        _pool.IsCurrent(voice) && (_streams[voice.Index] != null || StateOf(_sources[voice.Index]) == SourceState.Playing);

    /// <inheritdoc />
    public void StopAll()
    {
        for (int i = 0; i < _sources.Length; i++)
        {
            if (_pool.IsBusy(i)) FreeVoice(i);
        }
    }

    /// <inheritdoc />
    public void Update()
    {
        for (int i = 0; i < _sources.Length; i++)
        {
            if (!_pool.IsBusy(i)) continue;

            if (_streams[i] != null)
            {
                if (UpdateStream(i)) FreeVoice(i);
            }
            else if (StateOf(_sources[i]) != SourceState.Playing)
            {
                FreeVoice(i);
                continue;
            }

            ApplyGain(i);
        }
    }

    /// <summary>
    /// Stops every sound and frees all OpenAL objects, then closes the device. Sources are detached from their buffers first,
    /// because OpenAL refuses to delete a buffer that a source still references.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        for (int i = 0; i < _sources.Length; i++)
        {
            if (_pool.IsBusy(i)) FreeVoice(i);
        }

        foreach (uint source in _sources)
        {
            _al.SourceStop(source);
            _al.SetSourceProperty(source, SourceInteger.Buffer, 0);
            _al.DeleteSource(source);
        }

        foreach (uint buffer in _buffers.Values) _al.DeleteBuffer(buffer);
        _buffers.Clear();

        _alc.MakeContextCurrent(null);
        _alc.DestroyContext(_context);
        _alc.CloseDevice(_device);
        _al.Dispose();
        _alc.Dispose();
    }

    /// <summary>Takes a voice from the pool (cutting off the oldest when none is free), resets its source and records its mixing state.</summary>
    private VoiceId Begin(AudioGroup? group, float volume, out uint source)
    {
        var voice = _pool.Acquire(out bool stolen);
        if (stolen) ReleaseResources(voice.Index);

        source = _sources[voice.Index];
        _baseVolume[voice.Index] = Math.Max(volume, 0f);
        _group[voice.Index] = group ?? Mixer.Master;
        _al.SetSourceProperty(source, SourceFloat.Gain, EffectiveGain(voice.Index));
        _appliedGain[voice.Index] = EffectiveGain(voice.Index);
        return voice;
    }

    /// <summary>A plain sound is played "relative to the listener" at the origin, so moving the listener never changes how it sounds.</summary>
    private void MakeUnpositioned(uint source)
    {
        _al.SetSourceProperty(source, SourceBoolean.SourceRelative, true);
        _al.SetSourceProperty(source, SourceVector3.Position, 0f, 0f, 0f);
        _al.SetSourceProperty(source, SourceFloat.RolloffFactor, 0f);
    }

    private float EffectiveGain(int index) => _baseVolume[index] * (_group[index]?.EffectiveVolume ?? 1f);

    /// <summary>Pushes the voice's gain to OpenAL when a volume or group changed since last time.</summary>
    private void ApplyGain(int index)
    {
        float gain = EffectiveGain(index);
        if (gain == _appliedGain[index]) return;

        _al.SetSourceProperty(_sources[index], SourceFloat.Gain, gain);
        Check("applying a voice gain");
        _appliedGain[index] = gain;
    }

    /// <summary>Stops a voice, frees what it held (stream and its buffers) and returns it to the pool.</summary>
    private void FreeVoice(int index)
    {
        ReleaseResources(index);
        _pool.Release(index);
    }

    private void ReleaseResources(int index)
    {
        uint source = _sources[index];
        StopSource(source);

        var stream = _streams[index];
        if (stream == null) return;

        _al.SetSourceProperty(source, SourceInteger.Buffer, 0); // detach the queued buffers
        foreach (uint buffer in stream.Buffers) _al.DeleteBuffer(buffer);
        stream.Source.Dispose();
        _streams[index] = null;
        Check("freeing a stream");
    }

    private uint BufferFor(AudioClip clip)
    {
        if (_buffers.TryGetValue(clip, out uint existing)) return existing;

        uint buffer = _al.GenBuffer();
        Check("creating a buffer");
        var format = clip.Channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;
        fixed (byte* data = clip.Pcm16)
            _al.BufferData(buffer, format, data, clip.Pcm16.Length, clip.SampleRate);
        Check("uploading a clip");

        _buffers[clip] = buffer;
        return buffer;
    }

    private void StopSource(uint source)
    {
        _al.SourceStop(source);
        Check("stopping a source");
    }

    private SourceState StateOf(uint source)
    {
        _al.GetSourceProperty(source, GetSourceInteger.SourceState, out int state);
        return (SourceState)state;
    }
}
