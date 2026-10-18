using Engine.Physics2D;

namespace Engine.Physics2D.Tests;

public class MovingPlatformMathTests
{
    [Fact]
    public void Advances_forward_at_the_given_speed()
    {
        var (distance, returning) = MovingPlatformMath.Advance(10f, false, 100f, 32f, 0.5f);

        Assert.Equal(26f, distance);
        Assert.False(returning);
    }

    [Fact]
    public void Advances_backward_while_returning()
    {
        var (distance, returning) = MovingPlatformMath.Advance(50f, true, 100f, 32f, 0.5f);

        Assert.Equal(34f, distance);
        Assert.True(returning);
    }

    [Fact]
    public void Reaching_the_far_end_exactly_keeps_going_forward_until_the_next_step()
    {
        var (distance, returning) = MovingPlatformMath.Advance(90f, false, 100f, 20f, 0.5f);

        Assert.Equal(100f, distance);
        Assert.False(returning);
    }

    [Fact]
    public void Overshooting_the_far_end_reflects_and_turns_around()
    {
        var (distance, returning) = MovingPlatformMath.Advance(96f, false, 100f, 32f, 0.5f);

        Assert.Equal(88f, distance); // 4 to the end, 12 back
        Assert.True(returning);
    }

    [Fact]
    public void Overshooting_the_start_reflects_and_heads_out_again()
    {
        var (distance, returning) = MovingPlatformMath.Advance(4f, true, 100f, 32f, 0.5f);

        Assert.Equal(12f, distance);
        Assert.False(returning);
    }

    [Fact]
    public void A_step_longer_than_the_path_still_lands_on_the_path()
    {
        var (distance, _) = MovingPlatformMath.Advance(0f, false, 10f, 1000f, 1f); // 1000 units on a 10 unit path

        Assert.InRange(distance, 0f, 10f);
    }

    [Theory]
    [InlineData(0f, 10f)]
    [InlineData(-5f, 10f)]
    [InlineData(100f, 0f)]
    [InlineData(100f, -3f)]
    public void No_length_or_no_speed_means_no_movement(float length, float speed)
    {
        var (distance, returning) = MovingPlatformMath.Advance(0f, false, length, speed, 1f);

        Assert.Equal(0f, distance);
        Assert.False(returning);
    }

    [Fact]
    public void A_distance_outside_the_path_is_clamped_when_standing_still()
    {
        var (distance, _) = MovingPlatformMath.Advance(150f, false, 100f, 0f, 1f);

        Assert.Equal(100f, distance);
    }
}
