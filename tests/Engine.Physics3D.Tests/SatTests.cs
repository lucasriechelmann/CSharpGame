using System.Numerics;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class SatTests
{
    private static Obb3 Box(Vector3 centre, Vector3 half, Quaternion? rotation = null) => new(centre, half, rotation ?? Quaternion.Identity);

    private static Quaternion Turn(Vector3 axis, float degrees) => Quaternion.CreateFromAxisAngle(axis, degrees * MathF.PI / 180f);

    [Fact]
    public void Two_aligned_boxes_overlapping_along_x_are_pushed_apart_along_x_by_the_overlap()
    {
        var a = Box(new Vector3(1.5f, 0, 0), new Vector3(1, 1, 1)); // spans 0.5 .. 2.5
        var b = Box(Vector3.Zero, new Vector3(1, 1, 1));            // spans -1 .. 1

        Assert.True(Sat.TryPenetration(a, b, out var normal, out float depth));

        Assert.Equal(Vector3.UnitX, normal);
        Assert.Equal(0.5f, depth, 4);
    }

    [Fact]
    public void The_push_direction_points_from_the_second_box_to_the_first()
    {
        var a = Box(new Vector3(-1.5f, 0, 0), new Vector3(1, 1, 1));
        var b = Box(Vector3.Zero, new Vector3(1, 1, 1));

        Assert.True(Sat.TryPenetration(a, b, out var normal, out _));

        Assert.Equal(-Vector3.UnitX, normal);
    }

    [Fact]
    public void Separated_boxes_do_not_collide()
    {
        var a = Box(new Vector3(3, 0, 0), new Vector3(1, 1, 1));
        var b = Box(Vector3.Zero, new Vector3(1, 1, 1));

        Assert.False(Sat.TryPenetration(a, b, out _, out _));
    }

    [Fact]
    public void Boxes_that_only_touch_do_not_collide()
    {
        var a = Box(new Vector3(2, 0, 0), new Vector3(1, 1, 1));
        var b = Box(Vector3.Zero, new Vector3(1, 1, 1));

        Assert.False(Sat.TryPenetration(a, b, out _, out _));
    }

    [Fact]
    public void A_box_sitting_on_a_ramp_is_pushed_along_the_ramps_face_normal()
    {
        var ramp = Box(Vector3.Zero, new Vector3(5, 0.5f, 5), Turn(Vector3.UnitZ, 30f)); // tilted 30 degrees about Z
        // A unit box a little way into the ramp's upper face, above its middle.
        var faceNormal = Vector3.Transform(Vector3.UnitY, ramp.Rotation);
        var crate = Box(faceNormal * 0.9f, new Vector3(0.5f, 0.5f, 0.5f));

        Assert.True(Sat.TryPenetration(crate, ramp, out var normal, out float depth));

        Assert.True(Vector3.Dot(normal, faceNormal) > 0.99f);
        Assert.True(depth > 0f);
    }

    [Fact]
    public void A_box_below_a_rotated_slab_is_pushed_down_not_up()
    {
        var slab = Box(Vector3.Zero, new Vector3(5, 0.5f, 5), Turn(Vector3.UnitZ, 20f));
        var crate = Box(new Vector3(0, -0.9f, 0), new Vector3(0.5f, 0.5f, 0.5f));

        Assert.True(Sat.TryPenetration(crate, slab, out var normal, out _));

        Assert.True(normal.Y < 0f);
    }

    [Fact]
    public void A_diamond_turned_45_degrees_near_a_box_corner_separates_where_the_axis_aligned_bounds_would_not()
    {
        var diamond = Box(new Vector3(2.35f, 2.35f, 0), new Vector3(1, 1, 1), Turn(Vector3.UnitZ, 45f)); // its corner points at the box
        var box = Box(Vector3.Zero, new Vector3(1, 1, 1));

        // The diamond's bounding box (1.4 half-width) reaches 0.94 < 1 on both axes, so the AABBs overlap, but the shapes do not.
        Assert.False(Sat.TryPenetration(diamond, box, out _, out _));
    }

    [Fact]
    public void An_edge_to_edge_overlap_is_found_through_a_cross_product_axis()
    {
        // Two boxes turned 45 degrees about different axes, edges crossing: only an edge-edge axis separates or confirms them.
        var a = Box(new Vector3(0, 0, 0), new Vector3(1, 1, 1), Turn(Vector3.UnitX, 45f));
        var b = Box(new Vector3(0, 1.35f, 0), new Vector3(1, 1, 1), Turn(Vector3.UnitZ, 45f));

        Assert.True(Sat.TryPenetration(a, b, out var normal, out float depth));

        Assert.True(depth > 0f);
        Assert.Equal(1f, normal.Length(), 4);
    }

    [Fact]
    public void The_result_is_symmetric_up_to_direction()
    {
        var a = Box(new Vector3(1.2f, 0.3f, 0), new Vector3(1, 1, 1), Turn(Vector3.UnitY, 30f));
        var b = Box(Vector3.Zero, new Vector3(1, 0.5f, 1.5f), Turn(Vector3.UnitZ, 10f));

        Assert.True(Sat.TryPenetration(a, b, out var n1, out float d1));
        Assert.True(Sat.TryPenetration(b, a, out var n2, out float d2));

        Assert.Equal(d1, d2, 4);
        Assert.Equal(0f, (n1 + n2).Length(), 3);
    }

    // ---- Obb3 ----

    [Fact]
    public void An_oriented_boxs_bounds_grow_when_it_is_turned()
    {
        var box = Box(Vector3.Zero, new Vector3(1, 1, 1), Turn(Vector3.UnitZ, 45f));

        var bounds = box.Bounds;

        Assert.Equal(MathF.Sqrt(2f), bounds.Max.X, 4);
        Assert.Equal(MathF.Sqrt(2f), bounds.Max.Y, 4);
        Assert.Equal(1f, bounds.Max.Z, 4);
    }

    [Fact]
    public void A_world_point_round_trips_through_the_boxs_local_space()
    {
        var box = Box(new Vector3(3, 1, -2), new Vector3(1, 2, 3), Turn(Vector3.UnitY, 70f));
        var world = new Vector3(4.5f, 2f, -1f);

        var local = box.ToLocal(world);
        var back = Vector3.Transform(local, box.Rotation) + box.Center;

        Assert.Equal(world.X, back.X, 4);
        Assert.Equal(world.Y, back.Y, 4);
        Assert.Equal(world.Z, back.Z, 4);
    }

    [Fact]
    public void An_axis_index_outside_0_to_2_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Box(Vector3.Zero, Vector3.One).Axis(3));
    }

    [Fact]
    public void Inflating_grows_every_half_extent()
    {
        var grown = Box(Vector3.Zero, new Vector3(1, 2, 3)).Inflated(0.5f);

        Assert.Equal(new Vector3(1.5f, 2.5f, 3.5f), grown.HalfExtents);
    }

    [Fact]
    public void An_oriented_collider_turns_its_offset_with_it()
    {
        var collider = new BoxCollider3D { Size = new Vector3(2, 2, 2), Offset = new Vector3(1, 0, 0), Oriented = true };

        var obb = collider.GetObb(new Vector3(10, 0, 0), Turn(Vector3.UnitY, 90f));

        Assert.Equal(10f, obb.Center.X, 4);
        Assert.Equal(-1f, obb.Center.Z, 4); // +X turned a quarter about Y points along -Z
    }
}
