using Engine.Audio.Tests.Fixtures;

namespace Engine.Audio.Tests;

public class WavStreamTests
{
    private static WavStream Stream(int channels, byte[] data, int bits = 16, bool extraChunk = false) =>
        new(new MemoryStream(WavBuilder.Pcm(channels, 22050, bits, data, extraChunk)));

    [Fact]
    public void Reports_the_format_and_reads_the_samples_in_pieces()
    {
        var data = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(); // 4 stereo frames
        using var stream = Stream(2, data);
        var buffer = new byte[8];

        Assert.Equal(22050, stream.SampleRate);
        Assert.Equal(2, stream.Channels);
        Assert.Equal(8, stream.Read(buffer));
        Assert.Equal(data[..8], buffer);
        Assert.Equal(8, stream.Read(buffer));
        Assert.Equal(data[8..], buffer);
        Assert.Equal(0, stream.Read(buffer));
    }

    [Fact]
    public void Reads_only_whole_frames()
    {
        using var stream = Stream(2, new byte[16]);

        Assert.Equal(12, stream.Read(new byte[14])); // 14 bytes fit 3 whole 4-byte frames
    }

    [Fact]
    public void Rewind_starts_again_from_the_first_sample()
    {
        using var stream = Stream(1, new byte[] { 1, 0, 2, 0 });
        var buffer = new byte[4];
        stream.Read(buffer);
        Assert.Equal(0, stream.Read(buffer));

        stream.Rewind();

        Assert.Equal(4, stream.Read(buffer));
        Assert.Equal(new byte[] { 1, 0, 2, 0 }, buffer);
    }

    [Fact]
    public void Skips_chunks_it_does_not_know()
    {
        using var stream = Stream(1, new byte[] { 5, 0, 6, 0 }, extraChunk: true);
        var buffer = new byte[4];

        Assert.Equal(4, stream.Read(buffer));
        Assert.Equal(new byte[] { 5, 0, 6, 0 }, buffer);
    }

    [Fact]
    public void Eight_bit_and_non_wav_data_are_rejected_with_a_reason()
    {
        var eight = Assert.Throws<InvalidDataException>(() => Stream(1, new byte[4], bits: 8));
        Assert.Contains("16-bit", eight.Message);
        Assert.Throws<InvalidDataException>(() => new WavStream(new MemoryStream(new byte[] { 1, 2, 3 })));
    }

    [Fact]
    public void Disposing_closes_the_underlying_stream()
    {
        var inner = new MemoryStream(WavBuilder.Pcm(1, 8000, 16, new byte[4]));
        var stream = new WavStream(inner);

        stream.Dispose();

        Assert.False(inner.CanRead);
    }
}
