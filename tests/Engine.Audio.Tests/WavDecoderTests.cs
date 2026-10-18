using Engine.Audio.Tests.Fixtures;

namespace Engine.Audio.Tests;

public class WavDecoderTests
{
    [Fact]
    public void Decodes_16_bit_mono()
    {
        byte[] samples = [0x01, 0x00, 0xFF, 0x7F]; // 1, 32767

        var clip = WavDecoder.Decode(WavBuilder.Pcm(1, 8000, 16, samples));

        Assert.Equal(8000, clip.SampleRate);
        Assert.Equal(1, clip.Channels);
        Assert.Equal(2, clip.FrameCount);
        Assert.Equal(samples, clip.Pcm16);
    }

    [Fact]
    public void Decodes_16_bit_stereo_frames_and_duration()
    {
        var samples = new byte[4 * 4]; // 4 stereo frames

        var clip = WavDecoder.Decode(WavBuilder.Pcm(2, 4, 16, samples));

        Assert.Equal(2, clip.Channels);
        Assert.Equal(4, clip.FrameCount);
        Assert.Equal(1.0, clip.Duration); // 4 frames at 4 Hz
    }

    [Fact]
    public void Widens_8_bit_unsigned_to_16_bit_signed()
    {
        byte[] samples = [128, 255, 0]; // silence, max, min

        var clip = WavDecoder.Decode(WavBuilder.Pcm(1, 8000, 8, samples));

        Assert.Equal(3, clip.FrameCount);
        Assert.Equal(0, BitConverter.ToInt16(clip.Pcm16, 0));
        Assert.Equal(127 << 8, BitConverter.ToInt16(clip.Pcm16, 2));
        Assert.Equal(-128 << 8, BitConverter.ToInt16(clip.Pcm16, 4));
    }

    [Fact]
    public void Skips_unknown_chunks_including_odd_length_padding()
    {
        byte[] samples = [0x10, 0x00];

        var clip = WavDecoder.Decode(WavBuilder.Pcm(1, 8000, 16, samples, extraChunkBeforeData: true));

        Assert.Equal(samples, clip.Pcm16);
    }

    [Fact]
    public void Rejects_a_file_without_a_riff_header()
    {
        var ex = Assert.Throws<InvalidDataException>(() => WavDecoder.Decode(new byte[40]));

        Assert.Contains("RIFF", ex.Message);
    }

    [Fact]
    public void Rejects_compressed_encodings()
    {
        var file = WavBuilder.Pcm(1, 8000, 16, new byte[4], format: 3); // IEEE float

        var ex = Assert.Throws<InvalidDataException>(() => WavDecoder.Decode(file));

        Assert.Contains("encoding 3", ex.Message);
    }

    [Fact]
    public void Rejects_unsupported_bit_depth()
    {
        var file = WavBuilder.Pcm(1, 8000, 24, new byte[6]);

        var ex = Assert.Throws<InvalidDataException>(() => WavDecoder.Decode(file));

        Assert.Contains("24", ex.Message);
    }

    [Fact]
    public void Rejects_more_than_two_channels()
    {
        var file = WavBuilder.Pcm(6, 8000, 16, new byte[12]);

        Assert.Throws<InvalidDataException>(() => WavDecoder.Decode(file));
    }

    [Fact]
    public void Rejects_a_truncated_data_chunk()
    {
        var file = WavBuilder.Pcm(1, 8000, 16, new byte[8]);
        var truncated = file[..^4];

        var ex = Assert.Throws<InvalidDataException>(() => WavDecoder.Decode(truncated));

        Assert.Contains("truncated", ex.Message);
    }

    [Fact]
    public void Rejects_a_file_with_no_data_chunk()
    {
        var file = WavBuilder.Pcm(1, 8000, 16, new byte[4])[..36]; // RIFF header + fmt chunk only

        var ex = Assert.Throws<InvalidDataException>(() => WavDecoder.Decode(file));

        Assert.Contains("no 'data'", ex.Message);
    }

    [Fact]
    public void Rejects_an_empty_data_chunk()
    {
        var file = WavBuilder.Pcm(1, 8000, 16, Array.Empty<byte>());

        var ex = Assert.Throws<InvalidDataException>(() => WavDecoder.Decode(file));

        Assert.Contains("empty", ex.Message);
    }
}
