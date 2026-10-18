using System.Buffers.Binary;

namespace Engine.Audio;

/// <summary>
/// Streams a 16-bit PCM <c>.wav</c> file: it reads the header once, then hands out the samples of the <c>data</c> chunk piece by piece,
/// so a long music track never sits in memory whole. Eight-bit files are not streamed (decode them with <see cref="WavDecoder"/>).
/// </summary>
public sealed class WavStream : IAudioStream
{
    private readonly Stream _stream;
    private readonly long _dataStart;
    private readonly long _dataLength;
    private long _consumed;

    /// <summary>Opens a stream over a WAV file's bytes. The stream must be seekable and is owned (disposed) by this object.</summary>
    /// <exception cref="InvalidDataException">The data is not a 16-bit PCM WAV file or is truncated before its data chunk.</exception>
    public WavStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek) throw new ArgumentException("A streamed WAV must be seekable so it can loop.", nameof(stream));

        _stream = stream;
        try
        {
            ReadHeader(out int channels, out int sampleRate, out _dataStart, out _dataLength);
            Channels = channels;
            SampleRate = sampleRate;
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        _stream.Position = _dataStart;
    }

    /// <inheritdoc />
    public int SampleRate { get; }

    /// <inheritdoc />
    public int Channels { get; }

    /// <summary>Opens a WAV file for streaming.</summary>
    public static WavStream Open(string fullPath) => new(File.OpenRead(fullPath));

    /// <inheritdoc />
    public int Read(Span<byte> buffer)
    {
        int frame = Channels * sizeof(short);
        long left = _dataLength - _consumed;
        int wanted = (int)Math.Min(buffer.Length - buffer.Length % frame, left - left % frame);
        if (wanted <= 0) return 0;

        int read = 0;
        while (read < wanted)
        {
            int n = _stream.Read(buffer.Slice(read, wanted - read));
            if (n == 0) break; // the file is shorter than its header claimed
            read += n;
        }

        read -= read % frame;
        _consumed += read;
        return read;
    }

    /// <inheritdoc />
    public void Rewind()
    {
        _stream.Position = _dataStart;
        _consumed = 0;
    }

    /// <inheritdoc />
    public void Dispose() => _stream.Dispose();

    private void ReadHeader(out int channels, out int sampleRate, out long dataStart, out long dataLength)
    {
        Span<byte> head = stackalloc byte[12];
        if (_stream.Read(head) < 12 || !Tag(head, 0, "RIFF") || !Tag(head, 8, "WAVE"))
            throw new InvalidDataException("Not a WAV file: missing the RIFF/WAVE header.");

        channels = 0;
        sampleRate = 0;
        bool sawFormat = false;
        Span<byte> chunk = stackalloc byte[8];
        while (_stream.Read(chunk) == 8)
        {
            int size = BinaryPrimitives.ReadInt32LittleEndian(chunk[4..]);
            if (size < 0) throw new InvalidDataException("WAV chunk with a negative size (corrupt file).");

            if (Tag(chunk, 0, "fmt "))
            {
                if (size < 16) throw new InvalidDataException($"WAV 'fmt ' chunk is {size} bytes; at least 16 are required.");
                var fmt = new byte[size];
                _stream.ReadExactly(fmt);
                ushort format = BinaryPrimitives.ReadUInt16LittleEndian(fmt);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(2));
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(fmt.AsSpan(4));
                int bits = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(14));
                if (format == 0xFFFE && size >= 26) format = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(24));
                if (format != 1) throw new InvalidDataException($"Unsupported WAV encoding {format}; only uncompressed PCM (1) is supported.");
                if (channels is not (1 or 2)) throw new InvalidDataException($"Unsupported WAV channel count {channels}; only mono and stereo are supported.");
                if (bits != 16) throw new InvalidDataException($"Streaming supports 16-bit WAV only (this file is {bits}-bit).");
                if (sampleRate <= 0) throw new InvalidDataException($"Invalid WAV sample rate {sampleRate}.");
                sawFormat = true;
                if ((size & 1) != 0) _stream.Seek(1, SeekOrigin.Current);
            }
            else if (Tag(chunk, 0, "data"))
            {
                if (!sawFormat) throw new InvalidDataException("WAV 'data' chunk appears before the 'fmt ' chunk.");
                dataStart = _stream.Position;
                dataLength = Math.Min(size, _stream.Length - dataStart);
                if (dataLength <= 0) throw new InvalidDataException("WAV 'data' chunk is empty.");
                return;
            }
            else
            {
                _stream.Seek(size + (size & 1), SeekOrigin.Current);
            }
        }

        throw new InvalidDataException(sawFormat ? "WAV file has no 'data' chunk." : "WAV file has no 'fmt ' chunk.");
    }

    private static bool Tag(ReadOnlySpan<byte> bytes, int offset, string tag)
    {
        for (int i = 0; i < 4; i++)
        {
            if (bytes[offset + i] != tag[i]) return false;
        }

        return true;
    }
}
