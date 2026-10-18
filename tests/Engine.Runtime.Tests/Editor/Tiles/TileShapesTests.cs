using Engine.Runtime.Editor.Tiles;
using Engine.Tilemaps;

namespace Engine.Runtime.Tests.Editor.Tiles;

public class TileShapesTests
{
    [Fact]
    public void A_rectangle_covers_both_corners_whichever_order_they_come_in()
    {
        var forward = TileShapes.Rectangle(1, 1, 3, 2).ToList();
        var backward = TileShapes.Rectangle(3, 2, 1, 1).ToList();

        Assert.Equal(6, forward.Count);
        Assert.Equal(forward.OrderBy(c => c).ToList(), backward.OrderBy(c => c).ToList());
        Assert.Contains((1, 1), forward);
        Assert.Contains((3, 2), forward);
    }

    [Fact]
    public void A_single_cell_rectangle_is_that_cell()
    {
        Assert.Equal([(4, 5)], TileShapes.Rectangle(4, 5, 4, 5).ToList());
    }

    [Fact]
    public void A_fill_covers_the_connected_equal_region_and_stops_at_other_tiles()
    {
        var layer = new TileLayer(16f);
        for (int y = 0; y < 3; y++) layer.SetTile(2, y, 9); // a wall splitting the area

        var cells = TileShapes.FloodFill(layer, 0, 1, 0, 0, 4, 2);

        Assert.Equal(6, cells.Count);                        // columns 0 and 1, three rows
        Assert.DoesNotContain((2, 1), cells);
        Assert.DoesNotContain((3, 1), cells);
    }

    [Fact]
    public void A_fill_of_empty_space_is_held_by_the_limit()
    {
        var layer = new TileLayer(16f);

        var cells = TileShapes.FloodFill(layer, 0, 0, -1, -1, 1, 1);

        Assert.Equal(9, cells.Count);
    }

    [Fact]
    public void A_fill_does_not_leak_diagonally()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(1, 0, 5);
        layer.SetTile(0, 1, 5);

        var cells = TileShapes.FloodFill(layer, 0, 0, 0, 0, 2, 2);

        Assert.Equal([(0, 0)], cells); // (1,1) touches only diagonally, and both neighbours are walls
    }

    [Fact]
    public void A_fill_starting_outside_the_limit_changes_nothing()
    {
        Assert.Empty(TileShapes.FloodFill(new TileLayer(16f), 9, 9, 0, 0, 2, 2));
    }

    [Fact]
    public void A_fill_is_capped()
    {
        var cells = TileShapes.FloodFill(new TileLayer(16f), 0, 0, 0, 0, 1000, 1000, maxCells: 50);

        Assert.Equal(50, cells.Count);
    }

    [Fact]
    public void Fill_compares_whole_raw_values_so_a_flipped_tile_is_a_different_tile()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(0, 0, 3);
        layer.SetTile(1, 0, TileId.With(3, TileId.FlipHorizontal));

        Assert.Single(TileShapes.FloodFill(layer, 0, 0, 0, 0, 1, 0));
    }
}
