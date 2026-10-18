using System.Numerics;
using Engine.Physics2D;

namespace Engine.Physics2D.Tests;

/// <summary>Uses a 10x10 grid with 1-unit tiles at the origin so expected distances are easy to read.</summary>
public class CollisionSolverTests
{
    private static TileGrid NewGrid() => new(10, 10, 1f, Vector2.Zero);

    private static Aabb Box(float centerX, float centerY) => Aabb.FromCenter(new Vector2(centerX, centerY), Vector2.One);

    private static void FillRow(TileGrid grid, int y)
    {
        for (int x = 0; x < grid.Width; x++) grid.SetSolid(x, y, true);
    }

    private static void FillColumn(TileGrid grid, int x)
    {
        for (int y = 0; y < grid.Height; y++) grid.SetSolid(x, y, true);
    }

    [Fact]
    public void Free_movement_is_not_limited()
    {
        var solver = new CollisionSolver(NewGrid());

        float moved = solver.Sweep(Box(5.5f, 5.5f), Axis.X, 2f, out bool hit);

        Assert.Equal(2f, moved, 3);
        Assert.False(hit);
    }

    [Fact]
    public void Zero_delta_moves_nothing_and_never_hits()
    {
        var grid = NewGrid();
        FillRow(grid, 0);

        float moved = new CollisionSolver(grid).Sweep(Box(5.5f, 1.5f), Axis.Y, 0f, out bool hit);

        Assert.Equal(0f, moved);
        Assert.False(hit);
    }

    [Fact]
    public void Moving_right_stops_at_a_wall()
    {
        var grid = NewGrid();
        FillColumn(grid, 7); // wall face at x = 7

        float moved = new CollisionSolver(grid).Sweep(Box(3.5f, 5.5f), Axis.X, 10f, out bool hit);

        Assert.Equal(3f, moved, 3); // box right edge starts at 4
        Assert.True(hit);
    }

    [Fact]
    public void Moving_left_stops_at_a_wall()
    {
        var grid = NewGrid();
        FillColumn(grid, 2); // wall face at x = 3

        float moved = new CollisionSolver(grid).Sweep(Box(6.5f, 5.5f), Axis.X, -10f, out bool hit);

        Assert.Equal(-3f, moved, 3);
        Assert.True(hit);
    }

    [Fact]
    public void Falling_stops_on_the_floor()
    {
        var grid = NewGrid();
        FillRow(grid, 0); // floor top at y = 1

        float moved = new CollisionSolver(grid).Sweep(Box(5.5f, 3.5f), Axis.Y, -5f, out bool hit);

        Assert.Equal(-2f, moved, 3); // box bottom starts at 3
        Assert.True(hit);
    }

    [Fact]
    public void Rising_stops_at_the_ceiling()
    {
        var grid = NewGrid();
        FillRow(grid, 8); // ceiling underside at y = 8

        float moved = new CollisionSolver(grid).Sweep(Box(5.5f, 5.5f), Axis.Y, 5f, out bool hit);

        Assert.Equal(2f, moved, 3); // box top starts at 6
        Assert.True(hit);
    }

    [Fact]
    public void Fast_movement_cannot_tunnel_through_a_thin_wall()
    {
        var grid = NewGrid();
        for (int y = 0; y < grid.Height; y++) grid.SetSolid(5, y, true); // one tile thick

        float moved = new CollisionSolver(grid).Sweep(Box(1.5f, 5.5f), Axis.X, 100f, out bool hit);

        Assert.Equal(3f, moved, 3);
        Assert.True(hit);
    }

    [Fact]
    public void Resting_on_the_floor_beside_a_wall_still_blocks_sideways_but_not_down_along_the_wall()
    {
        var grid = NewGrid();
        FillRow(grid, 0);
        FillColumn(grid, 7);
        var solver = new CollisionSolver(grid);
        var box = Box(6.5f, 1.5f); // sitting on the floor, right edge exactly touching the wall

        float sideways = solver.Sweep(box, Axis.X, 1f, out bool hitSide);
        float down = solver.Sweep(box, Axis.Y, -1f, out bool hitDown);

        Assert.Equal(0f, sideways, 3);
        Assert.True(hitSide);
        Assert.Equal(0f, down, 3);  // blocked by the floor, not by the wall it touches
        Assert.True(hitDown);
    }

    [Fact]
    public void Touching_a_wall_does_not_block_vertical_movement_in_open_air()
    {
        var grid = NewGrid();
        FillColumn(grid, 7);

        float moved = new CollisionSolver(grid).Sweep(Box(6.5f, 5.5f), Axis.Y, -3f, out bool hit);

        Assert.Equal(-3f, moved, 3);
        Assert.False(hit);
    }

    [Fact]
    public void A_box_already_overlapping_a_tile_can_leave_it()
    {
        var grid = NewGrid();
        grid.SetSolid(5, 5, true);

        float moved = new CollisionSolver(grid).Sweep(Box(5.5f, 5.5f), Axis.X, 2f, out bool hit);

        Assert.Equal(2f, moved, 3);
        Assert.False(hit);
    }

    [Fact]
    public void Outside_the_grid_is_open_space()
    {
        float moved = new CollisionSolver(NewGrid()).Sweep(Box(9.5f, 5.5f), Axis.X, 50f, out bool hit);

        Assert.Equal(50f, moved, 3);
        Assert.False(hit);
    }

    [Fact]
    public void Static_boxes_block_like_tiles()
    {
        var solver = new CollisionSolver(null);
        solver.StaticBoxes.Add(new Aabb(new Vector2(5, 0), new Vector2(6, 10)));

        float moved = solver.Sweep(Box(2.5f, 5.5f), Axis.X, 10f, out bool hit);

        Assert.Equal(2f, moved, 3);
        Assert.True(hit);
    }

    [Fact]
    public void Static_boxes_beside_the_path_do_not_block()
    {
        var solver = new CollisionSolver(null);
        solver.StaticBoxes.Add(new Aabb(new Vector2(5, 8), new Vector2(6, 9))); // above the moving box

        float moved = solver.Sweep(Box(2.5f, 2.5f), Axis.X, 10f, out bool hit);

        Assert.Equal(10f, moved, 3);
        Assert.False(hit);
    }

    [Fact]
    public void The_nearest_obstacle_wins_across_tiles_and_boxes()
    {
        var grid = NewGrid();
        FillColumn(grid, 8); // face at 8
        var solver = new CollisionSolver(grid);
        solver.StaticBoxes.Add(new Aabb(new Vector2(6, 0), new Vector2(7, 10))); // face at 6, nearer

        float moved = solver.Sweep(Box(2.5f, 5.5f), Axis.X, 10f, out bool hit);

        Assert.Equal(3f, moved, 3);
        Assert.True(hit);
    }

    [Fact]
    public void Sweeping_with_no_solids_at_all_returns_the_full_delta()
    {
        float moved = new CollisionSolver(null).Sweep(Box(0, 0), Axis.Y, -7f, out bool hit);

        Assert.Equal(-7f, moved, 3);
        Assert.False(hit);
    }
}
