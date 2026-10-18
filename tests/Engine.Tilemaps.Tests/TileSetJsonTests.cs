using Engine.Core.Assets;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Engine.Tilemaps.Tests;

public class TileSetJsonTests
{
    private static readonly AssetHandle<Texture2D> NoTexture = AssetHandle<Texture2D>.Invalid;

    [Fact]
    public void Reads_the_description()
    {
        var data = TileSetJson.Read("""
            { "texture": "tiles.png", "tileWidth": 16, "tileHeight": 16, "solid": [1, 2, 3] }
            """);

        Assert.Equal("tiles.png", data.Texture);
        Assert.Equal(16, data.TileWidth);
        Assert.Equal(new[] { 1, 2, 3 }, data.Solid.ToArray());
        Assert.Null(data.Columns);
    }

    [Fact]
    public void Columns_and_rows_come_from_the_atlas_size_when_omitted()
    {
        var data = TileSetJson.Read("""{ "texture": "t.png", "tileWidth": 16, "tileHeight": 16 }""");

        var tiles = data.CreateTileSet(NoTexture, atlasWidth: 80, atlasHeight: 16);

        Assert.Equal(5, tiles.Columns);
        Assert.Equal(1, tiles.Rows);
        Assert.Equal(5, tiles.TileCount);
    }

    [Fact]
    public void Spacing_and_margin_are_accounted_for_when_computing_the_grid()
    {
        // 1 px margin, 2 px spacing, 16 px tiles: 3 columns need 1 + 3*16 + 2*2 + 1 = 54 px.
        var data = TileSetJson.Read("""{ "texture": "t.png", "tileWidth": 16, "tileHeight": 16, "spacing": 2, "margin": 1 }""");

        var tiles = data.CreateTileSet(NoTexture, atlasWidth: 54, atlasHeight: 36);

        Assert.Equal(3, tiles.Columns);
        Assert.Equal(2, tiles.Rows);
        Assert.Equal(new Rect(1 + 18, 1, 16, 16), tiles.GetSourceRect(2));
    }

    [Fact]
    public void Explicit_columns_and_rows_win_over_the_atlas_size()
    {
        var data = TileSetJson.Read("""{ "texture": "t.png", "tileWidth": 16, "tileHeight": 16, "columns": 2, "rows": 2 }""");

        var tiles = data.CreateTileSet(NoTexture, atlasWidth: 1000, atlasHeight: 1000);

        Assert.Equal(4, tiles.TileCount);
    }

    [Fact]
    public void Solid_ids_become_solid_tiles()
    {
        var data = TileSetJson.Read("""{ "texture": "t.png", "tileWidth": 16, "tileHeight": 16, "solid": [1, 3] }""");

        var tiles = data.CreateTileSet(NoTexture, 80, 16);

        Assert.True(tiles.IsSolid(1));
        Assert.False(tiles.IsSolid(2));
        Assert.True(tiles.IsSolid(3));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "tileWidth": 16, "tileHeight": 16 }""")]                         // no texture
    [InlineData("""{ "texture": "", "tileWidth": 16, "tileHeight": 16 }""")]
    public void Invalid_files_are_rejected_when_read(string json)
    {
        Assert.Throws<InvalidOperationException>(() => TileSetJson.Read(json));
    }

    [Theory]
    [InlineData("""{ "texture": "t.png", "tileWidth": 0, "tileHeight": 16 }""")]     // zero tile size
    [InlineData("""{ "texture": "t.png", "tileWidth": 16, "tileHeight": 16, "solid": [99] }""")] // id outside the set
    [InlineData("""{ "texture": "t.png", "tileWidth": 64, "tileHeight": 64 }""")]    // atlas too small
    public void Inconsistent_descriptions_are_rejected_when_the_tile_set_is_built(string json)
    {
        var data = TileSetJson.Read(json);

        Assert.Throws<InvalidOperationException>(() => data.CreateTileSet(NoTexture, 80, 16));
    }
}
