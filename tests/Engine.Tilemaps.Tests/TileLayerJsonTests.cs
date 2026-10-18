using Engine.Tilemaps;

namespace Engine.Tilemaps.Tests;

public class TileLayerJsonTests
{
    [Fact]
    public void Rows_are_listed_top_first_and_the_origin_is_the_bottom_left_cell()
    {
        var layer = TileLayerJson.Read("""
            { "tileSize": 32, "originX": 2, "originY": 5,
              "rows": [ [1, 0, 3],
                        [4, 5, 6] ] }
            """);

        // Bottom row is y = 5, top row y = 6.
        Assert.Equal(4, layer.GetTile(2, 5));
        Assert.Equal(5, layer.GetTile(3, 5));
        Assert.Equal(6, layer.GetTile(4, 5));
        Assert.Equal(1, layer.GetTile(2, 6));
        Assert.Equal(0, layer.GetTile(3, 6));
        Assert.Equal(3, layer.GetTile(4, 6));
        Assert.Equal(32f, layer.TileSize);
    }

    [Fact]
    public void Negative_origins_are_supported()
    {
        var layer = TileLayerJson.Read("""{ "tileSize": 16, "originX": -3, "originY": -2, "rows": [ [7] ] }""");

        Assert.Equal(7, layer.GetTile(-3, -2));
    }

    [Fact]
    public void Origin_defaults_to_zero_and_rows_may_be_ragged()
    {
        var layer = TileLayerJson.Read("""{ "tileSize": 16, "rows": [ [1], [2, 3] ] }""");

        Assert.Equal(1, layer.GetTile(0, 1));
        Assert.Equal(2, layer.GetTile(0, 0));
        Assert.Equal(3, layer.GetTile(1, 0));
        Assert.Equal(0, layer.GetTile(1, 1));
    }

    [Fact]
    public void A_sparse_layer_with_negative_cells_survives_a_write_and_read()
    {
        var original = new TileLayer(32f);
        original.SetTile(-20, 3, 5);
        original.SetTile(40, -7, 9);
        original.SetTile(0, 0, 1);

        var copy = TileLayerJson.Read(TileLayerJson.Write(original));

        Assert.Equal(32f, copy.TileSize);
        Assert.Equal(3, copy.TileCount);
        Assert.Equal(5, copy.GetTile(-20, 3));
        Assert.Equal(9, copy.GetTile(40, -7));
        Assert.Equal(1, copy.GetTile(0, 0));
        Assert.Equal(0, copy.GetTile(1, 0));
    }

    [Fact]
    public void An_empty_layer_writes_no_rows_and_reads_back_empty()
    {
        string json = TileLayerJson.Write(new TileLayer(8f));

        var copy = TileLayerJson.Read(json);

        Assert.Equal(0, copy.TileCount);
        Assert.Equal(8f, copy.TileSize);
    }

    [Fact]
    public void Written_files_read_like_a_map_one_row_per_line()
    {
        var layer = new TileLayer(32f);
        layer.SetTile(0, 0, 3);
        layer.SetTile(1, 0, 3);
        layer.SetTile(0, 1, 2);

        string json = TileLayerJson.Write(layer);

        Assert.Contains("[2, 0]", json); // top row first
        Assert.Contains("[3, 3]", json);
        Assert.True(json.IndexOf("[2, 0]", StringComparison.Ordinal) < json.IndexOf("[3, 3]", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "rows": [ [1] ] }""")]                                 // no tile size
    [InlineData("""{ "tileSize": 0, "rows": [ [1] ] }""")]
    [InlineData("""{ "tileSize": 32, "rows": [ [-1] ] }""")]                // negative id
    [InlineData("""{ "tileSize": 32, "rows": [ ["a"] ] }""")]               // not a number
    public void Invalid_files_are_rejected_with_InvalidOperationException(string json)
    {
        Assert.Throws<InvalidOperationException>(() => TileLayerJson.Read(json));
    }
}
