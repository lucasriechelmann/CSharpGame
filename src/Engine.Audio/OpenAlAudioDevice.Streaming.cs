using Silk.NET.OpenAL;

namespace Engine.Audio;

// Streamed sounds: a few small buffers are queued on a source and refilled from an IAudioStream as OpenAL finishes them.
public sealed unsafe partial class OpenAlAudioDevice
{
    /// <summary>How many buffers a stream keeps queued: enough that one slow frame does not run the source dry.</summary>
    private const int StreamBufferCount = 4;

    /// <summary>Bytes read from the stream per buffer (about 0.19 s of 44.1 kHz stereo).</summary>
    private const int StreamChunkBytes = 32768;

    /// <inheritdoc />
    public VoiceId PlayStream(IAudioStream stream, AudioGroup? group = null, float volume = 1f, bool loop = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);

        var voice = Begin(group, volume, out uint source);
        var state = new StreamVoice(stream, loop, _al.GenBuffers(StreamBufferCount));
        _streams[voice.Index] = state;

        _al.SetSourceProperty(source, SourceInteger.Buffer, 0);
        _al.SetSourceProperty(source, SourceFloat.Pitch, 1f);
        _al.SetSourceProperty(source, SourceBoolean.Looping, false); // looping is done by rewinding the stream, not by OpenAL
        MakeUnpositioned(source);
        Check("configuring a stream source");

        foreach (uint buffer in state.Buffers)
        {
            if (!Fill(state, buffer)) break;
            uint queued = buffer;
            _al.SourceQueueBuffers(source, 1, &queued);
        }

        Check("queueing stream buffers");
        _al.SourcePlay(source);
        Check("starting a stream");
        return voice;
    }

    /// <summary>Refills finished buffers and restarts a source that ran dry. Returns true when the stream is over and every buffer has played.</summary>
    private bool UpdateStream(int index)
    {
        var state = _streams[index]!;
        uint source = _sources[index];

        _al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out int processed);
        Check("reading processed stream buffers");
        while (processed-- > 0)
        {
            uint buffer = 0;
            _al.SourceUnqueueBuffers(source, 1, &buffer);
            Check("unqueueing a stream buffer");
            if (state.Ended || !Fill(state, buffer)) continue;

            _al.SourceQueueBuffers(source, 1, &buffer);
            Check("requeueing a stream buffer");
        }

        _al.GetSourceProperty(source, GetSourceInteger.BuffersQueued, out int queued);
        Check("reading queued stream buffers");
        if (queued == 0) return state.Ended;

        if (StateOf(source) != SourceState.Playing) _al.SourcePlay(source); // underrun: the game stalled longer than the queue lasts
        Check("updating a stream");
        return false;
    }

    /// <summary>Reads the next chunk (rewinding a looping stream at its end) into a buffer. False when the stream has ended.</summary>
    private bool Fill(StreamVoice state, uint buffer)
    {
        int bytes = state.Source.Read(_streamScratch);
        if (bytes == 0 && state.Loop)
        {
            state.Source.Rewind();
            bytes = state.Source.Read(_streamScratch);
        }

        if (bytes == 0)
        {
            state.Ended = true;
            return false;
        }

        var format = state.Source.Channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;
        fixed (byte* data = _streamScratch)
            _al.BufferData(buffer, format, data, bytes, state.Source.SampleRate);
        Check("uploading stream data");
        return true;
    }

    private sealed class StreamVoice(IAudioStream source, bool loop, uint[] buffers)
    {
        public IAudioStream Source { get; } = source;

        public bool Loop { get; } = loop;

        public uint[] Buffers { get; } = buffers;

        public bool Ended { get; set; }
    }
}
