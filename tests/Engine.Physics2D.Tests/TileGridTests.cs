using System.Numerics;
using Engine.Physics2D;

namespace Engine.Physics2D.Tests;

public class TileGridTests
{
    private static TileGrid NewGrid() => new(10, 8, 32f, new Vector2(-160, -128));

    [Fact]
    public void New_grid_is_empty()
    {
        var grid = NewGrid();

        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                Assert.False(grid.IsSolid(x, y));
    }

    [Fact]
    public void SetSolid_marks_only_that_cell()
    {
        var grid = NewGrid();
        grid.SetSolid(3, 2, true);

        Assert.True(grid.IsSolid(3, 2));
        Assert.False(grid.IsSolid(2, 3));

        grid.SetSolid(3, 2, false);
        Assert.False(grid.IsSolid(3, 2));
    }

    [Fact]
    public void Cells_outside_the_grid_are_empty_and_cannot_be_set()
    {
        var grid = NewGrid();

        Assert.False(grid.IsSolid(-1, 0));
        Assert.False(grid.IsSolid(0, 8));
        Assert.False(grid.IsSolid(10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetSolid(10, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetSolid(0, -1, true));
    }

    [Theory]
    [InlineData(-160f, 0)]    // exactly on the first cell's left edge
    [InlineData(-129f, 0)]
    [InlineData(-128f, 1)]    // exactly on a boundary belongs to the cell on its right
    [InlineData(0f, 5)]
    [InlineData(-160.5f, -1)] // left of the grid gives a negative index, not 0
    [InlineData(159.9f, 9)]
    [InlineData(160f, 10)]
    public void CellX_maps_world_coordinates_to_columns(float worldX, int expected)
    {
        Assert.Equal(expected, NewGrid().CellX(worldX));
    }

    [Fact]
    public void CellY_uses_the_vertical_origin()
    {
        var grid = NewGrid();

        Assert.Equal(0, grid.CellY(-128f));
        Assert.Equal(4, grid.CellY(0f));
        Assert.Equal(-1, grid.CellY(-129f));
    }

    [Fact]
    public void Cell_bounds_and_centre_are_in_world_space()
    {
        var grid = NewGrid();
        var bounds = grid.CellBounds(1, 2);

        Assert.Equal(new Vector2(-128, -64), bounds.Min);
        Assert.Equal(new Vector2(-96, -32), bounds.Max);
        Assert.Equal(new Vector2(-112, -48), grid.CellCenter(1, 2));
    }

    [Fact]
    public void Invalid_dimensions_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileGrid(0, 1, 1f, Vector2.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileGrid(1, 0, 1f, Vector2.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileGrid(1, 1, 0f, Vector2.Zero));
    }
}
