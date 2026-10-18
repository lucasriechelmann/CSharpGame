using System.Numerics;

namespace Engine.Rendering.Tests;

public class PixelPerfectTests
{
    [Theory]
    [InlineData(480, 270, 1920, 1080, 4f, 0, 0, 1920, 1080)]
    [InlineData(480, 270, 1280, 720, 2f, 160, 90, 960, 540)] // 2x fits; the rest becomes bars
    [InlineData(480, 270, 1000, 1000, 2f, 20, 230, 960, 540)]
    [InlineData(320, 180, 320, 180, 1f, 0, 0, 320, 180)]
    public void The_layout_uses_the_largest_whole_scale_and_centres_the_image(
        int vw, int vh, int ww, int wh, float scale, int x, int y, int width, int height)
    {
        var layout = PixelPerfectLayout.Compute(vw, vh, ww, wh);

        Assert.Equal(new PixelPerfectLayout(scale, x, y, width, height), layout);
    }

    [Fact]
    public void A_window_smaller_than_the_image_shrinks_it_to_fit_keeping_the_shape()
    {
        var layout = PixelPerfectLayout.Compute(480, 270, 240, 200);

        Assert.Equal(0.5f, layout.Scale);
        Assert.Equal(240, layout.Width);
        Assert.Equal(135, layout.Height);
        Assert.Equal(32, layout.Y);
    }

    [Fact]
    public void Window_and_game_positions_convert_both_ways()
    {
        var layout = PixelPerfectLayout.Compute(480, 270, 1280, 720); // 2x, offset (160, 90)

        Assert.Equal(new Vector2(0, 0), layout.WindowToGame(new Vector2(160, 90)));
        Assert.Equal(new Vector2(240, 135), layout.WindowToGame(new Vector2(640, 360)));
        Assert.Equal(new Vector2(640, 360), layout.GameToWindow(new Vector2(240, 135)));
        Assert.True(layout.WindowToGame(new Vector2(10, 10)).X < 0); // in the bars, outside the image
    }

    [Fact]
    public void Nonpositive_sizes_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PixelPerfectLayout.Compute(0, 270, 100, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => PixelPerfectLayout.Compute(480, 270, 100, 0));
    }

    [Fact]
    public void Snap_rounds_to_whole_pixels_of_the_zoomed_view_only_when_enabled()
    {
        var camera = new Camera2D { Zoom = 4f };

        Assert.Equal(new Vector2(1.3f, 2.6f), camera.Snap(new Vector2(1.3f, 2.6f)));

        camera.PixelSnap = true;
        var snapped = camera.Snap(new Vector2(1.3f, 2.6f)); // a pixel is 0.25 world units at zoom 4

        Assert.Equal(1.25f, snapped.X);
        Assert.Equal(2.5f, snapped.Y);
    }

    [Fact]
    public void A_snapping_camera_builds_its_view_from_the_snapped_position()
    {
        var plain = new Camera2D { Zoom = 2f, Position = new Vector2(10.3f, 5f), ViewportSize = new Vector2(100, 100) };
        var snapping = new Camera2D { Zoom = 2f, Position = new Vector2(10.3f, 5f), ViewportSize = new Vector2(100, 100), PixelSnap = true };

        Assert.Equal(new Vector2(10.5f, 5f), snapping.ViewPosition);
        Assert.NotEqual(plain.GetViewProjection(), snapping.GetViewProjection());
        Assert.Equal(new Camera2D { Zoom = 2f, Position = new Vector2(10.5f, 5f), ViewportSize = new Vector2(100, 100) }.GetViewProjection(), snapping.GetViewProjection());
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(16, true)]
    [InlineData(3, false)]
    [InlineData(0, false)]
    public void Only_power_of_two_sample_counts_are_valid_formats(int samples, bool valid)
    {
        var format = new RenderTargetFormat(Samples: samples);

        if (valid) format.Validate();
        else Assert.Throws<ArgumentOutOfRangeException>(format.Validate);
    }

    [Fact]
    public void The_default_format_is_plain_8_bit_with_no_extra_buffers()
    {
        var format = RenderTargetFormat.Default;

        Assert.False(format.Hdr);
        Assert.False(format.Depth);
        Assert.False(format.IsMultisampled);
        Assert.Equal(TextureFilter.Linear, format.Filter);
    }
}
