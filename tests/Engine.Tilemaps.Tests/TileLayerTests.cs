using Engine.Tilemaps;

namespace Engine.Tilemaps.Tests;

public class TileLayerTests
{
    private static TileLayer NewLayer() => new(32f);

    [Fact]
    public void Empty_cells_read_as_zero()
    {
        var layer = NewLayer();

        Assert.Equal(0, layer.GetTile(0, 0));
        Assert.Equal(0, layer.GetTile(-500, 900));
        Assert.Equal(0, layer.ChunkCount);
    }

    [Fact]
    public void Placed_tiles_can_be_read_back()
    {
        var layer = NewLayer();
        layer.SetTile(3, 4, 7);

        Assert.Equal(7, layer.GetTile(3, 4));
        Assert.Equal(0, layer.GetTile(4, 3));
        Assert.Equal(1, layer.TileCount);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 15)]   // last cell of the first chunk
    [InlineData(16, 16)]   // first cell of the next chunk
    [InlineData(-1, -1)]   // last cell of the chunk below and left of the origin
    [InlineData(-16, -16)]
    [InlineData(-17, 31)]
    [InlineData(1000, -1000)]
    public void Tiles_round_trip_across_chunk_boundaries_and_negative_coordinates(int x, int y)
    {
        var layer = NewLayer();
        layer.SetTile(x, y, 5);

        Assert.Equal(5, layer.GetTile(x, y));
        Assert.Equal(1, layer.TileCount);
    }

    [Fact]
    public void Neighbouring_cells_in_different_chunks_do_not_interfere()
    {
        var layer = NewLayer();
        layer.SetTile(15, 0, 1);
        layer.SetTile(16, 0, 2);
        layer.SetTile(-1, 0, 3);

        Assert.Equal(1, layer.GetTile(15, 0));
        Assert.Equal(2, layer.GetTile(16, 0));
        Assert.Equal(3, layer.GetTile(-1, 0));
        Assert.Equal(3, layer.ChunkCount);
    }

    [Fact]
    public void Chunks_are_created_lazily_and_shared_by_nearby_tiles()
    {
        var layer = NewLayer();
        layer.SetTile(1, 1, 1);
        layer.SetTile(2, 2, 1);
        layer.SetTile(14, 14, 1);

        Assert.Equal(1, layer.ChunkCount);
    }

    [Fact]
    public void Clearing_the_last_tile_releases_the_chunk()
    {
        var layer = NewLayer();
        layer.SetTile(5, 5, 9);
        layer.SetTile(6, 5, 9);

        layer.SetTile(5, 5, 0);
        Assert.Equal(1, layer.ChunkCount);

        layer.SetTile(6, 5, 0);
        Assert.Equal(0, layer.ChunkCount);
        Assert.Equal(0, layer.TileCount);
    }

    [Fact]
    public void Clearing_an_empty_cell_does_nothing()
    {
        var layer = NewLayer();

        layer.SetTile(100, 100, 0);

        Assert.Equal(0, layer.ChunkCount);
    }

    [Fact]
    public void Overwriting_a_tile_keeps_the_count_correct()
    {
        var layer = NewLayer();
        layer.SetTile(2, 2, 1);
        layer.SetTile(2, 2, 4);

        Assert.Equal(4, layer.GetTile(2, 2));
        Assert.Equal(1, layer.TileCount);
    }

    [Fact]
    public void Negative_ids_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewLayer().SetTile(0, 0, -1));
    }

    [Fact]
    public void Non_positive_tile_size_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileLayer(0f));
    }

    [Fact]
    public void Bounds_cover_all_tiles_including_negative_ones()
    {
        var layer = NewLayer();
        layer.SetTile(-20, 3, 1);
        layer.SetTile(40, -7, 1);
        layer.SetTile(0, 0, 1);

        Assert.True(layer.TryGetBounds(out int minX, out int minY, out int maxX, out int maxY));
        Assert.Equal((-20, -7, 40, 3), (minX, minY, maxX, maxY));
    }

    [Fact]
    public void An_empty_layer_has_no_bounds()
    {
        Assert.False(NewLayer().TryGetBounds(out _, out _, out _, out _));
    }

    [Fact]
    public void Chunk_tiles_are_exposed_row_major_for_the_renderer()
    {
        var layer = NewLayer();
        layer.SetTile(2, 3, 8); // local x 2, local y 3 in chunk (0,0)

        Assert.True(layer.TryGetChunkTiles(0, 0, out var tiles));
        Assert.Equal(TileLayer.ChunkSize * TileLayer.ChunkSize, tiles.Length);
        Assert.Equal(8, tiles[3 * TileLayer.ChunkSize + 2]);
    }

    [Fact]
    public void Chunk_tiles_of_a_negative_chunk_start_at_its_own_origin()
    {
        var layer = NewLayer();
        layer.SetTile(-1, -1, 6); // last cell of chunk (-1,-1): local (15,15)

        Assert.True(layer.TryGetChunkTiles(-1, -1, out var tiles));
        Assert.Equal(6, tiles[15 * TileLayer.ChunkSize + 15]);
    }

    [Fact]
    public void Missing_chunks_report_false()
    {
        Assert.False(NewLayer().TryGetChunkTiles(3, 3, out _));
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(31.9f, 0)]
    [InlineData(32f, 1)]
    [InlineData(-0.1f, -1)]
    [InlineData(-32f, -1)]
    [InlineData(-32.1f, -2)]
    public void CellIndex_floors_world_coordinates(float local, int expected)
    {
        Assert.Equal(expected, NewLayer().CellIndex(local));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 0)]
    [InlineData(16, 1)]
    [InlineData(-1, -1)]
    [InlineData(-16, -1)]
    [InlineData(-17, -2)]
    public void ChunkIndex_floors_negative_cells(int cell, int expected)
    {
        Assert.Equal(expected, TileLayer.ChunkIndex(cell));
    }
}
