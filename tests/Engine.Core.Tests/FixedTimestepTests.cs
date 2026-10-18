using Engine.Core;

namespace Engine.Core.Tests;

// Step sizes are powers of two so the accumulator arithmetic is exact in binary floating point.
public class FixedTimestepTests
{
    [Fact]
    public void Runs_whole_steps_and_keeps_remainder()
    {
        var t = new FixedTimestep(0.125, maxFrameSeconds: 1);
        Assert.Equal(2, t.Advance(0.3125));
        Assert.Equal(0.5, t.Alpha, 6);
    }

    [Fact]
    public void Remainder_carries_over_between_frames()
    {
        var t = new FixedTimestep(0.125);
        Assert.Equal(0, t.Advance(0.0625));
        Assert.Equal(1, t.Advance(0.0625 + 0.0625));
    }

    [Fact]
    public void Long_stalls_are_clamped()
    {
        var t = new FixedTimestep(0.125, maxFrameSeconds: 0.375);
        Assert.Equal(3, t.Advance(10));
    }

    [Fact]
    public void Negative_time_is_ignored()
    {
        var t = new FixedTimestep(0.125);
        Assert.Equal(0, t.Advance(-1));
        Assert.Equal(0, t.Alpha);
    }
}
