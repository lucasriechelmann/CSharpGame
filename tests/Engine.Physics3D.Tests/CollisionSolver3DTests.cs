using System.Numerics;
using Engine.Core;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class CollisionSolver3DTests
{
    private static readonly Aabb3 Unit = Aabb3.FromCenter(Vector3.Zero, Vector3.One); // spans -0.5..0.5 on every axis

    private static CollisionSolver3D WithSolids(params Aabb3[] solids)
    {
        var solver = new CollisionSolver3D();
        solver.StaticBoxes.AddRange(solids);
        return solver;
    }

    [Theory]
    [InlineData(Axis3D.X)]
    [InlineData(Axis3D.Y)]
    [InlineData(Axis3D.Z)]
    public void Free_movement_is_allowed_on_every_axis(Axis3D axis)
    {
        var solver = WithSolids();

        float allowed = solver.Sweep(Unit, axis, 5f, out bool hit);

        Assert.Equal(5f, allowed);
        Assert.False(hit);
    }

    [Theory]
    [InlineData(Axis3D.X)]
    [InlineData(Axis3D.Y)]
    [InlineData(Axis3D.Z)]
    public void A_box_in_the_path_stops_the_sweep_flush_with_its_face(Axis3D axis)
    {
        // A wall whose near face is 2 units from our leading face, on whichever axis we move along.
        var wallCentre = AxisMath.Along(axis, 3f);
        var wall = Aabb3.FromCenter(wallCentre, new Vector3(1f, 1f, 1f));
        var solver = WithSolids(wall);

        float allowed = solver.Sweep(Unit, axis, 10f, out bool hit);

        Assert.Equal(2f, allowed, 4);
        Assert.True(hit);
    }

    [Fact]
    public void Moving_in_the_negative_direction_stops_at_the_far_face()
    {
        var floor = Aabb3.FromCenter(new Vector3(0f, -3f, 0f), new Vector3(10f, 1f, 10f)); // top face at y = -2.5
        var solver = WithSolids(floor);

        float allowed = solver.Sweep(Unit, Axis3D.Y, -10f, out bool hit);

        Assert.Equal(-2f, allowed, 4); // our bottom face is at -0.5, the floor top at -2.5
        Assert.True(hit);
    }

    [Fact]
    public void A_box_beside_the_path_does_not_block()
    {
        var beside = Aabb3.FromCenter(new Vector3(3f, 0f, 2f), Vector3.One); // clear of our Z extent
        var solver = WithSolids(beside);

        float allowed = solver.Sweep(Unit, Axis3D.X, 10f, out bool hit);

        Assert.Equal(10f, allowed);
        Assert.False(hit);
    }

    [Fact]
    public void Obstacles_overlapping_on_only_one_perpendicular_axis_do_not_block()
    {
        var above = Aabb3.FromCenter(new Vector3(3f, 2f, 0f), Vector3.One); // overlaps in Z, clear in Y

        float allowed = WithSolids(above).Sweep(Unit, Axis3D.X, 10f, out bool hit);

        Assert.Equal(10f, allowed);
        Assert.False(hit);
    }

    [Fact]
    public void A_thin_wall_cannot_be_tunnelled_through_at_any_speed()
    {
        var wall = Aabb3.FromCenter(new Vector3(5f, 0f, 0f), new Vector3(0.01f, 4f, 4f));
        var solver = WithSolids(wall);

        float allowed = solver.Sweep(Unit, Axis3D.X, 1000f, out bool hit);

        Assert.True(hit);
        Assert.Equal(4.495f, allowed, 3);
    }

    [Fact]
    public void Touching_a_face_does_not_block_sliding_along_it()
    {
        var floor = Aabb3.FromCenter(new Vector3(0f, -1f, 0f), new Vector3(20f, 1f, 20f)); // top face at y = -0.5, touching our bottom
        var solver = WithSolids(floor);

        float allowed = solver.Sweep(Unit, Axis3D.X, 3f, out bool hit);

        Assert.Equal(3f, allowed);
        Assert.False(hit);
    }

    [Fact]
    public void A_zero_move_reports_nothing()
    {
        var solver = WithSolids(Unit);

        Assert.Equal(0f, solver.Sweep(Unit, Axis3D.X, 0f, out bool hit));
        Assert.False(hit);
    }

    [Fact]
    public void An_obstacle_behind_the_leading_face_is_ignored()
    {
        // We start inside a big solid: its faces are behind our leading faces on the way out, so we may leave.
        var solver = WithSolids(Aabb3.FromCenter(Vector3.Zero, new Vector3(10f, 10f, 10f)));

        float allowed = solver.Sweep(Unit, Axis3D.X, 1f, out bool hit);

        Assert.Equal(1f, allowed);
        Assert.False(hit);
    }

    [Fact]
    public void Raycast_finds_the_nearest_box_and_the_face_normal()
    {
        var near = Aabb3.FromCenter(new Vector3(0f, 0f, -5f), Vector3.One * 2f);   // front face at z = -4
        var far = Aabb3.FromCenter(new Vector3(0f, 0f, -10f), Vector3.One * 2f);
        var solver = WithSolids(far, near);
        var ray = new Ray(Vector3.Zero, -Vector3.UnitZ);

        Assert.True(solver.Raycast(ray, 100f, out var hit));

        Assert.Equal(4f, hit.Distance, 4);
        Assert.Equal(1, hit.Index); // the nearer box was added second
        Assert.Equal(Vector3.UnitZ, hit.Normal);
        Assert.Equal(new Vector3(0f, 0f, -4f), hit.Point);
    }

    [Fact]
    public void Raycast_reports_the_top_face_of_a_box_hit_from_above()
    {
        var floor = Aabb3.FromCenter(new Vector3(0f, -1f, 0f), new Vector3(20f, 2f, 20f));
        var solver = WithSolids(floor);

        Assert.True(solver.Raycast(new Ray(new Vector3(3f, 10f, 2f), -Vector3.UnitY), 100f, out var hit));

        Assert.Equal(10f, hit.Distance, 4);
        Assert.Equal(Vector3.UnitY, hit.Normal);
    }

    [Fact]
    public void Raycast_beyond_the_maximum_distance_misses()
    {
        var solver = WithSolids(Aabb3.FromCenter(new Vector3(0f, 0f, -50f), Vector3.One));

        Assert.False(solver.Raycast(new Ray(Vector3.Zero, -Vector3.UnitZ), 10f, out _));
    }

    [Fact]
    public void Raycast_starting_inside_a_box_hits_at_zero_with_the_reverse_direction_as_normal()
    {
        var solver = WithSolids(Aabb3.FromCenter(Vector3.Zero, Vector3.One * 4f));
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);

        Assert.True(solver.Raycast(ray, 10f, out var hit));

        Assert.Equal(0f, hit.Distance);
        Assert.Equal(-Vector3.UnitX, hit.Normal);
    }

    [Fact]
    public void Raycast_picks_the_right_face_of_a_long_thin_box()
    {
        // A slab 20 wide and 0.2 tall: a ray from above must report the top face, not a side, even near the end.
        var slab = Aabb3.FromCenter(Vector3.Zero, new Vector3(20f, 0.2f, 4f));
        var solver = WithSolids(slab);

        Assert.True(solver.Raycast(new Ray(new Vector3(9.9f, 5f, 0f), -Vector3.UnitY), 20f, out var hit));

        Assert.Equal(Vector3.UnitY, hit.Normal);
    }
}
