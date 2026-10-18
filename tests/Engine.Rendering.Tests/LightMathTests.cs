using System.Numerics;

namespace Engine.Rendering.Tests;

public class LightMathTests
{
    [Fact]
    public void Falloff_is_full_at_the_centre_and_zero_at_and_beyond_the_edge()
    {
        Assert.Equal(1f, LightMath.Falloff(0f));
        Assert.Equal(0f, LightMath.Falloff(1f));
        Assert.Equal(0f, LightMath.Falloff(2.5f));
    }

    [Fact]
    public void Falloff_treats_negative_distance_like_the_centre()
    {
        Assert.Equal(1f, LightMath.Falloff(-0.5f));
    }

    [Fact]
    public void Falloff_halfway_is_the_squared_complement()
    {
        // (1 - 0.5^2)^2 = 0.5625, exactly representable.
        Assert.Equal(0.5625f, LightMath.Falloff(0.5f));
    }

    [Fact]
    public void Falloff_never_increases_with_distance()
    {
        float previous = LightMath.Falloff(0f);
        for (float d = 0.05f; d <= 1.05f; d += 0.05f)
        {
            float current = LightMath.Falloff(d);
            Assert.True(current <= previous, $"falloff rose at distance {d}");
            previous = current;
        }
    }

    [Fact]
    public void Gradient_has_the_requested_size_and_white_opaque_centre()
    {
        var pixels = LightMath.BuildGradientPixels(5);

        Assert.Equal(5 * 5 * 4, pixels.Length);
        int centre = (2 * 5 + 2) * 4;
        Assert.Equal(255, pixels[centre]);
        Assert.Equal(255, pixels[centre + 1]);
        Assert.Equal(255, pixels[centre + 2]);
        Assert.Equal(255, pixels[centre + 3]);
    }

    [Fact]
    public void Gradient_corners_are_fully_transparent()
    {
        var pixels = LightMath.BuildGradientPixels(8);

        Assert.Equal(0, pixels[3]);                          // top-left alpha
        Assert.Equal(0, pixels[(7 * 8 + 7) * 4 + 3]);        // bottom-right alpha
    }

    [Fact]
    public void Gradient_is_symmetric_left_to_right()
    {
        var pixels = LightMath.BuildGradientPixels(9);

        for (int y = 0; y < 9; y++)
            Assert.Equal(pixels[(y * 9 + 1) * 4 + 3], pixels[(y * 9 + 7) * 4 + 3]);
    }

    [Fact]
    public void Gradient_rejects_a_tiny_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LightMath.BuildGradientPixels(1));
    }

    [Theory]
    [InlineData(0f, 0f, 10f, true)]      // inside the view
    [InlineData(115f, 0f, 20f, true)]    // centre just outside, circle reaches in
    [InlineData(130f, 0f, 20f, false)]   // circle stops short of the right edge
    [InlineData(-130f, 0f, 20f, false)]
    [InlineData(0f, 80f, 20f, false)]    // above the top edge (view is 100 x 60)
    [InlineData(0f, -80f, 20f, false)]
    public void Overlaps_tests_the_circle_against_the_visible_box(float x, float y, float radius, bool expected)
    {
        var min = new Vector2(-100f, -30f);
        var max = new Vector2(100f, 30f);

        Assert.Equal(expected, LightMath.Overlaps(min, max, new Vector2(x, y), radius));
    }

    [Fact]
    public void Light_create_sets_all_fields()
    {
        var light = Light2D.Create(200f, Color.Red, 1.5f);

        Assert.Equal(200f, light.Radius);
        Assert.Equal(Color.Red, light.Color);
        Assert.Equal(1.5f, light.Intensity);
    }
}
