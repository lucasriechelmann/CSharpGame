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
    public VoiceId Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(clip);
        if (pitch <= 0f) throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "Pitch must be positive.");

        var voice = _pool.Acquire(out bool stolen);
        uint source = _sources[voice.Index];
        if (stolen) StopSource(source);

        _al.SetSourceProperty(source, SourceInteger.Buffer, (int)BufferFor(clip));
        _al.SetSourceProperty(source, SourceFloat.Gain, Math.Max(volume, 0f));
        _al.SetSourceProperty(source, SourceFloat.Pitch, pitch);
        _al.SetSourceProperty(source, SourceBoolean.Looping, loop);
        Check("configuring a source");

        _al.SourcePlay(source);
        Check("starting a source");
        return voice;
    }

    /// <inheritdoc />
    public void Stop(VoiceId voice)
    {
        if (!_pool.IsCurrent(voice)) return;

        StopSource(_sources[voice.Index]);
        _pool.Release(voice.Index);
    }

    /// <inheritdoc />
    public bool IsPlaying(VoiceId voice) => _pool.IsCurrent(voice) && StateOf(_sources[voice.Index]) == SourceState.Playing;

    /// <inheritdoc />
    public void StopAll()
    {
        for (int i = 0; i < _sources.Length; i++)
        {
            if (!_pool.IsBusy(i)) continue;

            StopSource(_sources[i]);
            _pool.Release(i);
        }
    }

    /// <inheritdoc />
    public void Update()
    {
        for (int i = 0; i < _sources.Length; i++)
        {
            if (_pool.IsBusy(i) && StateOf(_sources[i]) != SourceState.Playing) _pool.Release(i);
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

    /// <summary>Returns the clip's OpenAL buffer, uploading its samples on first use so later plays are free.</summary>
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
