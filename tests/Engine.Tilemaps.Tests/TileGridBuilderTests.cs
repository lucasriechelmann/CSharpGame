using System.Numerics;
using Engine.Core.Assets;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Engine.Tilemaps.Tests;

public class TileGridBuilderTests
{
    private const int Dirt = 1;
    private const int Decoration = 2;

    private static TileSet NewTileSet()
    {
        var tiles = new TileSet(AssetHandle<Texture2D>.Invalid, 16, 16, columns: 4, rows: 1);
        tiles.SetSolid(Dirt, true); // Decoration (id 2) stays non-solid
        return tiles;
    }

    [Fact]
    public void Grid_covers_the_layers_used_area_and_marks_solid_tiles()
    {
        var layer = new TileLayer(32f);
        layer.SetTile(2, 3, Dirt);
        layer.SetTile(5, 4, Dirt);

        var grid = TileGridBuilder.FromLayer(layer, NewTileSet(), Vector2.Zero);

        Assert.Equal(4, grid.Width);   // columns 2..5
        Assert.Equal(2, grid.Height);  // rows 3..4
        Assert.True(grid.IsSolid(0, 0));
        Assert.True(grid.IsSolid(3, 1));
        Assert.False(grid.IsSolid(1, 0));
    }

    [Fact]
    public void Grid_origin_accounts_for_the_layer_origin_and_the_first_used_cell()
    {
        var layer = new TileLayer(32f);
        layer.SetTile(2, 3, Dirt);

        var grid = TileGridBuilder.FromLayer(layer, NewTileSet(), new Vector2(-640f, -288f));

        Assert.Equal(new Vector2(-640f + 2 * 32f, -288f + 3 * 32f), grid.Origin);
        Assert.Equal(32f, grid.TileSize);
    }

    [Fact]
    public void Non_solid_tiles_leave_their_cells_empty()
    {
        var layer = new TileLayer(32f);
        layer.SetTile(0, 0, Dirt);
        layer.SetTile(1, 0, Decoration);

        var grid = TileGridBuilder.FromLayer(layer, NewTileSet(), Vector2.Zero);

        Assert.True(grid.IsSolid(0, 0));
        Assert.False(grid.IsSolid(1, 0));
    }

    [Fact]
    public void Solid_cells_are_where_the_world_says_they_are()
    {
        var layer = new TileLayer(32f);
        layer.SetTile(-3, -2, Dirt);

        var grid = TileGridBuilder.FromLayer(layer, NewTileSet(), new Vector2(100f, 100f));

        // Layer cell (-3,-2) spans x 4..36, y 36..68 in world space.
        var bounds = grid.CellBounds(0, 0);
        Assert.Equal(new Vector2(4f, 36f), bounds.Min);
        Assert.Equal(new Vector2(36f, 68f), bounds.Max);
        Assert.True(grid.IsSolid(0, 0));
    }

    [Fact]
    public void An_empty_layer_cannot_become_a_grid()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TileGridBuilder.FromLayer(new TileLayer(32f), NewTileSet(), Vector2.Zero));
    }
}
