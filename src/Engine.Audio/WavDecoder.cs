using System.Buffers.Binary;

namespace Engine.Audio;

/// <summary>
/// Reads uncompressed PCM <c>.wav</c> files (8- or 16-bit, mono or stereo) into an <see cref="AudioClip"/>. Written by hand because
/// the format is small and worth understanding: a RIFF container holding tagged chunks, of which only <c>fmt </c> (the format)
/// and <c>data</c> (the samples) matter here. Anything else is rejected with a message saying what was found.
/// </summary>
public static class WavDecoder
{
    private const ushort FormatPcm = 1;
    private const ushort FormatExtensible = 0xFFFE;

    /// <summary>Decodes a complete WAV file.</summary>
    /// <param name="file">The file's bytes.</param>
    /// <returns>A 16-bit clip (8-bit sources are widened).</returns>
    /// <exception cref="InvalidDataException">The data is not a PCM WAV file, uses an unsupported layout, or is truncated.</exception>
    public static AudioClip Decode(byte[] file)
    {
        if (file.Length < 12 || !Tag(file, 0, "RIFF") || !Tag(file, 8, "WAVE"))
            throw new InvalidDataException("Not a WAV file: missing the RIFF/WAVE header.");

        int channels = 0, sampleRate = 0, bits = 0;
        bool sawFormat = false;
        int position = 12;

        while (position + 8 <= file.Length)
        {
            int size = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(position + 4));
            int body = position + 8;
            if (size < 0 || body + size > file.Length)
                throw new InvalidDataException($"WAV chunk '{Text(file, position)}' claims {size} bytes but the file ends first (truncated file).");

            if (Tag(file, position, "fmt "))
            {
                ReadFormat(file, body, size, out channels, out sampleRate, out bits);
                sawFormat = true;
            }
            else if (Tag(file, position, "data"))
            {
                if (!sawFormat) throw new InvalidDataException("WAV 'data' chunk appears before the 'fmt ' chunk.");
                return BuildClip(file.AsSpan(body, size), channels, sampleRate, bits);
            }

            position = body + size + (size & 1); // chunks are padded to an even length
        }

        throw new InvalidDataException(sawFormat ? "WAV file has no 'data' chunk." : "WAV file has no 'fmt ' chunk.");
    }

    private static void ReadFormat(byte[] file, int body, int size, out int channels, out int sampleRate, out int bits)
    {
        if (size < 16) throw new InvalidDataException($"WAV 'fmt ' chunk is {size} bytes; at least 16 are required.");

        var span = file.AsSpan(body, size);
        ushort format = BinaryPrimitives.ReadUInt16LittleEndian(span);
        channels = BinaryPrimitives.ReadUInt16LittleEndian(span[2..]);
        sampleRate = BinaryPrimitives.ReadInt32LittleEndian(span[4..]);
        bits = BinaryPrimitives.ReadUInt16LittleEndian(span[14..]);

        if (format == FormatExtensible && size >= 26) format = BinaryPrimitives.ReadUInt16LittleEndian(span[24..]); // sub-format GUID starts with the real code
        if (format != FormatPcm) throw new InvalidDataException($"Unsupported WAV encoding {format}; only uncompressed PCM (1) is supported.");
        if (channels is not (1 or 2)) throw new InvalidDataException($"Unsupported WAV channel count {channels}; only mono and stereo are supported.");
        if (bits is not (8 or 16)) throw new InvalidDataException($"Unsupported WAV bit depth {bits}; only 8 and 16 are supported.");
        if (sampleRate <= 0) throw new InvalidDataException($"Invalid WAV sample rate {sampleRate}.");
    }

    private static AudioClip BuildClip(ReadOnlySpan<byte> data, int channels, int sampleRate, int bits)
    {
        if (data.IsEmpty) throw new InvalidDataException("WAV 'data' chunk is empty.");

        if (bits == 16) return new AudioClip(sampleRate, channels, data[..(data.Length - data.Length % (2 * channels))].ToArray());

        // 8-bit WAV is unsigned (silence = 128); widen to signed 16-bit so the device only ever sees one format.
        int usable = data.Length - data.Length % channels;
        var pcm = new byte[usable * 2];
        for (int i = 0; i < usable; i++)
            BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(i * 2), (short)((data[i] - 128) << 8));
        return new AudioClip(sampleRate, channels, pcm);
    }

    private static bool Tag(byte[] file, int offset, string tag)
    {
        if (offset + 4 > file.Length) return false;
        for (int i = 0; i < 4; i++)
            if (file[offset + i] != tag[i]) return false;
        return true;
    }

    private static string Text(byte[] file, int offset) => System.Text.Encoding.ASCII.GetString(file, offset, 4);
}
