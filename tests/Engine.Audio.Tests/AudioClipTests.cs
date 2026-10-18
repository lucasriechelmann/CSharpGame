namespace Engine.Audio.Tests;

public class AudioClipTests
{
    [Fact]
    public void Reports_frames_and_duration()
    {
        var clip = new AudioClip(8, 1, new byte[16]); // 8 mono frames at 8 Hz

        Assert.Equal(8, clip.FrameCount);
        Assert.Equal(1.0, clip.Duration);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(44100, 0)]
    [InlineData(44100, 3)]
    public void Rejects_bad_format_values(int rate, int channels)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AudioClip(rate, channels, new byte[8]));
    }

    [Fact]
    public void Rejects_empty_data()
    {
        Assert.Throws<ArgumentException>(() => new AudioClip(8000, 1, Array.Empty<byte>()));
    }

    [Fact]
    public void Rejects_data_that_ends_mid_frame()
    {
        Assert.Throws<ArgumentException>(() => new AudioClip(8000, 2, new byte[6])); // stereo 16-bit frames are 4 bytes
    }
}
