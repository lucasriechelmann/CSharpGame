using Engine.Animation;
using Engine.Rendering;

namespace Engine.Animation.Tests;

// Frame rates are powers of two so step boundaries land exactly on binary-representable times.
public class AnimationClipTests
{
    private static AnimationClip Clip(int frameCount, AnimationLoopMode mode, float fps = 4f) =>
        new("test", Enumerable.Range(0, frameCount).Select(i => new Rect(i * 16, 0, 16, 16)), fps, mode);

    private static int[] Sequence(AnimationClip clip, int steps)
    {
        float stepTime = 1f / clip.FramesPerSecond;
        return Enumerable.Range(0, steps).Select(i => clip.GetFrameIndex(i * stepTime, out _)).ToArray();
    }

    [Fact]
    public void Loop_wraps_back_to_the_first_frame()
    {
        Assert.Equal(new[] { 0, 1, 2, 0, 1, 2, 0 }, Sequence(Clip(3, AnimationLoopMode.Loop), 7));
    }

    [Fact]
    public void Loop_never_reports_finished()
    {
        var clip = Clip(3, AnimationLoopMode.Loop);

        clip.GetFrameIndex(1000f, out bool finished);

        Assert.False(finished);
    }

    [Fact]
    public void Once_holds_the_last_frame_and_reports_finished_after_the_clip_ends()
    {
        var clip = Clip(3, AnimationLoopMode.Once);

        Assert.Equal(2, clip.GetFrameIndex(0.5f, out bool midway));   // frame 2 at 0.5 s (4 fps)
        Assert.False(midway);
        Assert.Equal(2, clip.GetFrameIndex(0.75f, out bool atEnd));   // 3 frames take 0.75 s
        Assert.True(atEnd);
        Assert.Equal(2, clip.GetFrameIndex(10f, out bool later));
        Assert.True(later);
    }

    [Fact]
    public void PingPong_plays_forwards_then_backwards_without_repeating_the_end_frames()
    {
        Assert.Equal(new[] { 0, 1, 2, 3, 2, 1, 0, 1, 2 }, Sequence(Clip(4, AnimationLoopMode.PingPong), 9));
    }

    [Fact]
    public void PingPong_with_two_frames_alternates()
    {
        Assert.Equal(new[] { 0, 1, 0, 1 }, Sequence(Clip(2, AnimationLoopMode.PingPong), 4));
    }

    [Fact]
    public void A_single_frame_clip_always_shows_that_frame()
    {
        foreach (var mode in Enum.GetValues<AnimationLoopMode>())
            Assert.Equal(0, Clip(1, mode).GetFrameIndex(123f, out _));
    }

    [Fact]
    public void Negative_time_counts_as_zero()
    {
        Assert.Equal(0, Clip(3, AnimationLoopMode.Loop).GetFrameIndex(-5f, out _));
    }

    [Fact]
    public void Frame_rate_controls_how_long_each_frame_lasts()
    {
        var slow = Clip(4, AnimationLoopMode.Loop, fps: 2f);

        Assert.Equal(0, slow.GetFrameIndex(0.25f, out _));
        Assert.Equal(1, slow.GetFrameIndex(0.5f, out _));
        Assert.Equal(2f, slow.Duration);
    }

    [Fact]
    public void FromGrid_converts_cell_indices_to_pixel_rectangles()
    {
        var clip = AnimationClip.FromGrid("run", cellWidth: 16, cellHeight: 24, sheetColumns: 4, cells: [0, 3, 5], framesPerSecond: 8f);

        Assert.Equal(new Rect(0, 0, 16, 24), clip.Frames[0]);
        Assert.Equal(new Rect(48, 0, 16, 24), clip.Frames[1]);
        Assert.Equal(new Rect(16, 24, 16, 24), clip.Frames[2]); // cell 5: column 1, row 1
    }

    [Fact]
    public void Invalid_clips_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new AnimationClip("empty", [], 8f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationClip("bad", [new Rect(0, 0, 1, 1)], 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnimationClip.FromGrid("bad", 16, 16, 0, [0], 8f));
    }
}
