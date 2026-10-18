using System.Numerics;
using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Tilemaps.Tests;

public class TileShadowCasterTests
{
    private const int Dirt = 1;
    private const int Decoration = 2;

    private static TileSet NewTileSet()
    {
        var tiles = new TileSet(AssetHandle<Texture2D>.Invalid, 16, 16, columns: 4, rows: 1);
        tiles.SetSolid(Dirt, true);
        return tiles;
    }

    private static List<ShadowEdge> EdgesOf(TileLayer layer, Vector2 origin, int fromX, int fromY, int toX, int toY)
    {
        var edges = new List<ShadowEdge>();
        TileShadowCasterSource.AddCellRange(layer, NewTileSet(), origin, fromX, fromY, toX, toY, edges);
        return edges;
    }

    [Fact]
    public void A_lone_solid_tile_has_four_walls_facing_outward()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(2, 1, Dirt);

        var edges = EdgesOf(layer, Vector2.Zero, 0, 0, 4, 4);

        Assert.Equal(4, edges.Count);
        Assert.Contains(edges, e => e.OutwardNormal == new Vector2(0, -1) && e.A.Y == 16f); // bottom wall at y = 16
        Assert.Contains(edges, e => e.OutwardNormal == new Vector2(0, 1) && e.A.Y == 32f);  // top wall
    }

    [Fact]
    public void Walls_between_two_solid_tiles_are_left_out()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(0, 0, Dirt);
        layer.SetTile(1, 0, Dirt);

        var edges = EdgesOf(layer, Vector2.Zero, 0, 0, 1, 0);

        Assert.Equal(6, edges.Count); // 4 + 4 minus the shared pair
        Assert.DoesNotContain(edges, e => e.A.X == 16f && e.B.X == 16f); // nothing on the seam
    }

    [Fact]
    public void Decoration_and_empty_cells_cast_nothing()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(0, 0, Decoration);

        Assert.Empty(EdgesOf(layer, Vector2.Zero, -2, -2, 2, 2));
    }

    [Fact]
    public void The_layer_origin_moves_the_walls()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(0, 0, Dirt);

        var edges = EdgesOf(layer, new Vector2(100, 200), 0, 0, 0, 0);

        Assert.Equal(new Vector2(100, 200), edges[0].A);
    }

    [Fact]
    public void Only_cells_in_the_requested_range_are_read()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(0, 0, Dirt);
        layer.SetTile(10, 10, Dirt);

        Assert.Equal(4, EdgesOf(layer, Vector2.Zero, 0, 0, 3, 3).Count);
    }
}
