using Engine.Core.Assets;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Engine.Tilemaps.Tests;

public class TileSetTests
{
    private static readonly AssetHandle<Texture2D> NoTexture = AssetHandle<Texture2D>.Invalid;

    [Fact]
    public void Ids_run_left_to_right_then_top_to_bottom_starting_at_one()
    {
        var tiles = new TileSet(NoTexture, tileWidth: 16, tileHeight: 16, columns: 4, rows: 2);

        Assert.Equal(new Rect(0, 0, 16, 16), tiles.GetSourceRect(1));
        Assert.Equal(new Rect(48, 0, 16, 16), tiles.GetSourceRect(4));
        Assert.Equal(new Rect(0, 16, 16, 16), tiles.GetSourceRect(5));
        Assert.Equal(new Rect(48, 16, 16, 16), tiles.GetSourceRect(8));
        Assert.Equal(8, tiles.TileCount);
    }

    [Fact]
    public void Spacing_and_margin_offset_the_rectangles()
    {
        var tiles = new TileSet(NoTexture, 16, 16, columns: 3, rows: 2, spacing: 2, margin: 1);

        Assert.Equal(new Rect(1, 1, 16, 16), tiles.GetSourceRect(1));
        Assert.Equal(new Rect(1 + 18, 1, 16, 16), tiles.GetSourceRect(2));
        Assert.Equal(new Rect(1, 1 + 18, 16, 16), tiles.GetSourceRect(4));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-3)]
    public void Ids_outside_the_set_are_rejected(int id)
    {
        var tiles = new TileSet(NoTexture, 16, 16, columns: 4, rows: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => tiles.GetSourceRect(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => tiles.SetSolid(id, true));
    }

    [Fact]
    public void Solid_flags_are_per_tile_and_default_to_empty()
    {
        var tiles = new TileSet(NoTexture, 16, 16, columns: 4, rows: 2);
        tiles.SetSolid(2, true);

        Assert.True(tiles.IsSolid(2));
        Assert.False(tiles.IsSolid(3));

        tiles.SetSolid(2, false);
        Assert.False(tiles.IsSolid(2));
    }

    [Fact]
    public void Empty_and_unknown_ids_are_never_solid()
    {
        var tiles = new TileSet(NoTexture, 16, 16, columns: 4, rows: 2);

        Assert.False(tiles.IsSolid(0));
        Assert.False(tiles.IsSolid(99));
        Assert.False(tiles.IsSolid(-1));
    }

    [Fact]
    public void Invalid_dimensions_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileSet(NoTexture, 0, 16, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileSet(NoTexture, 16, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileSet(NoTexture, 16, 16, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileSet(NoTexture, 16, 16, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileSet(NoTexture, 16, 16, 1, 1, spacing: -1));
    }
}
