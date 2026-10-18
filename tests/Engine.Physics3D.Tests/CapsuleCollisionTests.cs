using System.Numerics;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class CapsuleCollisionTests
{
    private static readonly Aabb3 Unit = new(new Vector3(-1, -1, -1), new Vector3(1, 1, 1)); // a 2 x 2 x 2 box centred on the origin

    private static bool Hit(Vector3 bottom, Vector3 top, float radius, out Vector3 normal, out float depth) =>
        CapsuleCollision.TryPenetration(bottom, top, radius, Unit, out normal, out depth);

    [Fact]
    public void A_capsule_sinking_into_the_top_face_is_pushed_straight_up()
    {
        // The lower cap's centre is 0.25 above the top face and the radius is 0.5, so it is 0.25 deep.
        Assert.True(Hit(new Vector3(0, 1.25f, 0), new Vector3(0, 2.25f, 0), 0.5f, out var normal, out float depth));

        Assert.Equal(Vector3.UnitY, normal);
        Assert.Equal(0.25f, depth, 4);
    }

    [Fact]
    public void A_capsule_against_a_side_face_is_pushed_sideways()
    {
        Assert.True(Hit(new Vector3(1.3f, 0, 0), new Vector3(1.3f, 1, 0), 0.5f, out var normal, out float depth));

        Assert.Equal(Vector3.UnitX, normal);
        Assert.Equal(0.2f, depth, 4);
    }

    [Fact]
    public void Touching_without_overlap_is_not_a_collision()
    {
        Assert.False(Hit(new Vector3(1.5f, 0, 0), new Vector3(1.5f, 1, 0), 0.5f, out _, out _));
    }

    [Fact]
    public void A_capsule_further_away_does_not_collide()
    {
        Assert.False(Hit(new Vector3(5, 0, 0), new Vector3(5, 1, 0), 0.5f, out _, out _));
    }

    [Fact]
    public void A_capsule_on_a_box_corner_is_pushed_away_along_the_diagonal()
    {
        // The cap centre is off the corner (1, 1, 1) by 0.3 in X and 0.3 in Y: distance 0.424, radius 0.5.
        Assert.True(Hit(new Vector3(1.3f, 1.3f, 0), new Vector3(1.3f, 2.3f, 0), 0.5f, out var normal, out float depth));

        Assert.Equal(MathF.Sqrt(0.5f), normal.X, 3);
        Assert.Equal(MathF.Sqrt(0.5f), normal.Y, 3);
        Assert.Equal(0.5f - MathF.Sqrt(0.18f), depth, 3);
    }

    [Fact]
    public void A_tall_capsule_beside_a_short_box_touches_it_along_the_axis_not_just_at_the_ends()
    {
        // The segment runs from y = -3 to y = 3 past the box's side face; the closest point is in the middle of the segment.
        Assert.True(Hit(new Vector3(1.4f, -3, 0), new Vector3(1.4f, 3, 0), 0.5f, out var normal, out float depth));

        Assert.Equal(Vector3.UnitX, normal);
        Assert.Equal(0.1f, depth, 3);
    }

    [Fact]
    public void A_capsule_whose_axis_is_inside_the_box_leaves_by_the_nearest_face()
    {
        // Centre 0.9 from the +X face and 0 from the others: leaving through +X is shortest (0.1 plus the radius).
        Assert.True(Hit(new Vector3(0.9f, 0, 0), new Vector3(0.9f, 0.2f, 0), 0.25f, out var normal, out float depth));

        Assert.Equal(Vector3.UnitX, normal);
        Assert.Equal(1f - 0.9f + 0.25f, depth, 3);
    }

    [Fact]
    public void A_capsule_buried_below_the_middle_leaves_through_the_bottom()
    {
        Assert.True(Hit(new Vector3(0, -0.8f, 0), new Vector3(0, -0.6f, 0), 0.25f, out var normal, out _));

        Assert.Equal(-Vector3.UnitY, normal);
    }

    [Fact]
    public void A_sphere_is_a_capsule_with_a_zero_length_axis()
    {
        Assert.True(Hit(new Vector3(0, 1.25f, 0), new Vector3(0, 1.25f, 0), 0.5f, out var normal, out float depth));

        Assert.Equal(Vector3.UnitY, normal);
        Assert.Equal(0.25f, depth, 4);
    }

    [Fact]
    public void Closest_points_of_a_segment_passing_over_a_box_are_straight_above_it()
    {
        float distance = CapsuleCollision.ClosestPoints(new Vector3(-5, 3, 0), new Vector3(5, 3, 0), Unit, out var onSegment, out var onBox);

        Assert.Equal(2f, distance, 3);
        Assert.Equal(3f, onSegment.Y, 3);
        Assert.Equal(1f, onBox.Y, 3);
    }

    [Fact]
    public void The_distance_is_zero_when_the_segment_crosses_the_box()
    {
        Assert.Equal(0f, CapsuleCollision.ClosestPoints(new Vector3(-5, 0, 0), new Vector3(5, 0, 0), Unit, out _, out _), 4);
    }

    [Fact]
    public void A_zero_length_segment_is_just_a_point()
    {
        float distance = CapsuleCollision.ClosestPoints(new Vector3(4, 0, 0), new Vector3(4, 0, 0), Unit, out var onSegment, out _);

        Assert.Equal(3f, distance, 4);
        Assert.Equal(new Vector3(4, 0, 0), onSegment);
    }

    // ---- the collider component ----

    [Fact]
    public void A_capsules_axis_runs_between_the_cap_centres()
    {
        var capsule = CapsuleCollider3D.Create(0.5f, 2f);

        var (bottom, top) = capsule.GetSegment(new Vector3(1, 5, 1));

        Assert.Equal(new Vector3(1, 4.5f, 1), bottom);
        Assert.Equal(new Vector3(1, 5.5f, 1), top);
    }

    [Fact]
    public void A_sphere_has_a_zero_length_axis_and_a_cubic_bounding_box()
    {
        var sphere = CapsuleCollider3D.Sphere(0.5f);

        var (bottom, top) = sphere.GetSegment(Vector3.Zero);
        var bounds = sphere.GetBounds(Vector3.Zero);

        Assert.Equal(bottom, top);
        Assert.Equal(new Vector3(1, 1, 1), bounds.Size);
    }

    [Fact]
    public void A_height_below_two_radii_is_treated_as_a_sphere()
    {
        var squat = CapsuleCollider3D.Create(0.5f, 0.3f);

        Assert.Equal(0f, squat.HalfSegment);
    }

    [Fact]
    public void The_offset_moves_the_capsule_relative_to_the_entity()
    {
        var capsule = new CapsuleCollider3D { Radius = 0.5f, Height = 2f, Offset = new Vector3(0, 1, 0) };

        Assert.Equal(0f, capsule.GetBounds(Vector3.Zero).Min.Y, 4);
        Assert.Equal(2f, capsule.GetBounds(Vector3.Zero).Max.Y, 4);
    }
}
