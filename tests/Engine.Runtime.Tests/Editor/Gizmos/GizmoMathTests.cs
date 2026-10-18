using System.Numerics;
using Engine.Core;
using Engine.Runtime.Editor.Gizmos;

namespace Engine.Runtime.Tests.Editor.Gizmos;

public class GizmoMathTests
{
    [Fact]
    public void The_closest_point_on_an_axis_to_a_crossing_ray_is_where_they_cross()
    {
        // A ray straight down -Z at x = 3 passes the X axis (through the origin) at x = 3.
        var ray = new Ray(new Vector3(3, 0, 10), -Vector3.UnitZ);

        Assert.True(GizmoMath.TryClosestOnAxis(ray, Vector3.Zero, Vector3.UnitX, out float s));
        Assert.Equal(3f, s, 4);
    }

    [Fact]
    public void The_closest_point_accounts_for_a_ray_that_is_not_perpendicular_to_the_axis()
    {
        // Ray from (0, 0, 10) towards (3, 0, 0): it meets the X axis at x = 3.
        var ray = new Ray(new Vector3(0, 0, 10), Vector3.Normalize(new Vector3(3, 0, -10)));

        Assert.True(GizmoMath.TryClosestOnAxis(ray, Vector3.Zero, Vector3.UnitX, out float s));
        Assert.Equal(3f, s, 3);
    }

    [Fact]
    public void A_ray_parallel_to_the_axis_has_no_closest_point()
    {
        var ray = new Ray(new Vector3(0, 1, 0), Vector3.UnitX);

        Assert.False(GizmoMath.TryClosestOnAxis(ray, Vector3.Zero, Vector3.UnitX, out _));
    }

    [Fact]
    public void A_ray_crosses_a_plane_in_front_of_it()
    {
        var ray = new Ray(new Vector3(1, 2, 10), -Vector3.UnitZ);

        Assert.True(GizmoMath.TryIntersectPlane(ray, Vector3.Zero, Vector3.UnitZ, out var hit));
        Assert.Equal(new Vector3(1, 2, 0), hit);
    }

    [Fact]
    public void A_plane_behind_the_ray_or_parallel_to_it_is_not_hit()
    {
        var away = new Ray(new Vector3(0, 0, 10), Vector3.UnitZ);
        var parallel = new Ray(new Vector3(0, 0, 10), Vector3.UnitX);

        Assert.False(GizmoMath.TryIntersectPlane(away, Vector3.Zero, Vector3.UnitZ, out _));
        Assert.False(GizmoMath.TryIntersectPlane(parallel, Vector3.Zero, Vector3.UnitZ, out _));
    }

    [Fact]
    public void A_quarter_turn_counter_clockwise_about_z_is_a_positive_quarter_pi_times_two()
    {
        float angle = GizmoMath.SignedAngle(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

        Assert.Equal(MathF.PI / 2f, angle, 5);
        Assert.Equal(-MathF.PI / 2f, GizmoMath.SignedAngle(Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ), 5);
    }

    [Theory]
    [InlineData(5f, 3f, 3f)]    // beside the middle of the segment
    [InlineData(-4f, 3f, 5f)]   // beyond the first end: distance to that end
    [InlineData(14f, 0f, 4f)]   // beyond the second end
    public void Distance_to_a_segment_is_to_the_nearest_point_on_it(float x, float y, float expected)
    {
        Assert.Equal(expected, GizmoMath.DistanceToSegment(new Vector2(x, y), Vector2.Zero, new Vector2(10, 0)), 4);
    }

    [Fact]
    public void A_zero_length_segment_is_a_point()
    {
        Assert.Equal(5f, GizmoMath.DistanceToSegment(new Vector2(3, 4), Vector2.Zero, Vector2.Zero), 4);
    }

    [Theory]
    [InlineData(13f, 8f, 16f)]
    [InlineData(-3f, 8f, 0f)]
    [InlineData(5f, 0f, 5f)]   // no grid: untouched
    public void Snapping_rounds_to_the_nearest_step(float value, float step, float expected)
    {
        Assert.Equal(expected, GizmoMath.Snap(value, step), 4);
    }
}
