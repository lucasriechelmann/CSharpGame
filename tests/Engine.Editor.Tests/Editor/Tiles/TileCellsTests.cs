using System.Numerics;
using Engine.Editor.Tiles;

namespace Engine.Editor.Tests.Editor.Tiles;

public class TileCellsTests
{
    [Theory]
    [InlineData(0f, 0f, 0, 0)]
    [InlineData(31.9f, 31.9f, 0, 0)]
    [InlineData(32f, 64f, 1, 2)]
    [InlineData(-0.1f, -32.1f, -1, -2)]
    public void A_world_point_maps_to_its_cell(float x, float y, int cellX, int cellY)
    {
        Assert.Equal((cellX, cellY), TileCells.FromWorld(new Vector2(x, y), Vector2.Zero, 32f));
    }

    [Fact]
    public void The_layer_origin_shifts_the_cells()
    {
        // A layer placed at (-640, -288), like the sample's ground layer.
        Assert.Equal((0, 0), TileCells.FromWorld(new Vector2(-630f, -280f), new Vector2(-640f, -288f), 32f));
        Assert.Equal((19, 17), TileCells.FromWorld(new Vector2(-20f, 270f), new Vector2(-640f, -288f), 32f));
    }

    [Fact]
    public void A_non_positive_tile_size_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TileCells.FromWorld(Vector2.Zero, Vector2.Zero, 0f));
    }

    [Fact]
    public void Cell_bounds_span_one_tile_from_the_origin_corner()
    {
        var (min, max) = TileCells.Bounds(2, -1, new Vector2(100f, 50f), 16f);

        Assert.Equal(new Vector2(132f, 34f), min);
        Assert.Equal(new Vector2(148f, 50f), max);
    }

    [Fact]
    public void A_cell_maps_back_to_itself_through_its_bounds()
    {
        var (min, max) = TileCells.Bounds(5, 7, new Vector2(-640f, -288f), 32f);

        Assert.Equal((5, 7), TileCells.FromWorld((min + max) / 2f, new Vector2(-640f, -288f), 32f));
    }

    [Fact]
    public void A_line_to_the_same_cell_is_just_that_cell()
    {
        Assert.Equal(new[] { (3, 4) }, TileCells.Line(3, 4, 3, 4).ToArray());
    }

    [Fact]
    public void A_horizontal_line_visits_every_cell_in_order()
    {
        Assert.Equal(new[] { (0, 0), (1, 0), (2, 0), (3, 0) }, TileCells.Line(0, 0, 3, 0).ToArray());
    }

    [Fact]
    public void A_line_works_backwards_and_downwards()
    {
        Assert.Equal(new[] { (2, 2), (2, 1), (2, 0) }, TileCells.Line(2, 2, 2, 0).ToArray());
    }

    [Fact]
    public void A_diagonal_line_visits_the_diagonal()
    {
        Assert.Equal(new[] { (0, 0), (1, 1), (2, 2) }, TileCells.Line(0, 0, 2, 2).ToArray());
    }

    [Theory]
    [InlineData(0, 0, 9, 3)]
    [InlineData(5, 5, -4, 1)]
    [InlineData(-3, 2, -3, -8)]
    public void A_line_has_no_gaps_and_includes_both_ends(int x0, int y0, int x1, int y1)
    {
        var cells = TileCells.Line(x0, y0, x1, y1).ToArray();

        Assert.Equal((x0, y0), cells[0]);
        Assert.Equal((x1, y1), cells[^1]);
        Assert.Equal(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)) + 1, cells.Length);
        for (int i = 1; i < cells.Length; i++)
        {
            Assert.InRange(Math.Abs(cells[i].X - cells[i - 1].X), 0, 1);
            Assert.InRange(Math.Abs(cells[i].Y - cells[i - 1].Y), 0, 1);
        }
    }
}
