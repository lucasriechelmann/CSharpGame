using System.Numerics;
using Engine.Core.Assets;
using Engine.Physics2D;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Engine.Tilemaps.Tests;

public class TileFlagsAndAnimationTests
{
    private static readonly AssetHandle<Texture2D> NoTexture = AssetHandle<Texture2D>.Invalid;

    private static TileSet Set() => new(NoTexture, 16, 16, columns: 4, rows: 2);

    // ---- TileId ----

    [Fact]
    public void Id_and_flags_are_separate_parts_of_a_raw_value()
    {
        int raw = TileId.With(5, TileId.FlipHorizontal | TileId.FlipDiagonal);

        Assert.Equal(5, TileId.Id(raw));
        Assert.Equal(TileId.FlipHorizontal | TileId.FlipDiagonal, TileId.Flags(raw));
        Assert.True(raw > 0);
    }

    [Fact]
    public void A_plain_id_has_no_flags()
    {
        Assert.Equal(0, TileId.Flags(7));
        Assert.Equal(7, TileId.Id(7));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(TileId.MaxId + 1, 0)]
    [InlineData(1, 1)]          // a bit outside the flag mask
    public void Out_of_range_ids_and_stray_flag_bits_are_rejected(int id, int flags)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TileId.With(id, flags));
    }

    [Fact]
    public void Tiled_flip_bits_map_onto_ours()
    {
        const uint all = 0x80000000u | 0x40000000u | 0x20000000u;

        Assert.Equal(TileId.FlipHorizontal | TileId.FlipVertical | TileId.FlipDiagonal, TileId.FlagsFromTiled(all | 9u));
        Assert.Equal(TileId.FlipHorizontal, TileId.FlagsFromTiled(0x80000000u | 9u));
        Assert.Equal(9u, TileId.IdFromTiled(all | 9u));
        Assert.Equal(0, TileId.FlagsFromTiled(9u));
    }

    [Fact]
    public void A_layer_stores_and_returns_the_raw_value_with_its_flags()
    {
        var layer = new TileLayer(32f);
        int raw = TileId.With(3, TileId.FlipVertical);

        layer.SetTile(2, 2, raw);

        Assert.Equal(raw, layer.GetTile(2, 2));
        Assert.Equal(1, layer.TileCount);
    }

    [Fact]
    public void Solidity_and_the_source_rectangle_ignore_flip_flags()
    {
        var tiles = Set();
        tiles.SetSolid(3, true);
        int flipped = TileId.With(3, TileId.FlipHorizontal | TileId.FlipVertical);

        Assert.True(tiles.IsSolid(flipped));
        Assert.Equal(tiles.GetSourceRect(3), tiles.GetSourceRect(flipped));
    }

    [Fact]
    public void A_flipped_solid_tile_still_blocks_in_the_collision_grid()
    {
        var tiles = Set();
        tiles.SetSolid(2, true);
        var layer = new TileLayer(16f);
        layer.SetTile(0, 0, TileId.With(2, TileId.FlipDiagonal));
        layer.SetTile(1, 0, 1);

        var grid = TileGridBuilder.FromLayer(layer, tiles, Vector2.Zero);

        Assert.True(grid.IsSolid(0, 0));
        Assert.False(grid.IsSolid(1, 0));
    }

    [Fact]
    public void Layer_files_with_flipped_tiles_round_trip()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(0, 0, TileId.With(5, TileId.FlipHorizontal));
        layer.SetTile(1, 0, 6);

        var back = TileLayerJson.Read(TileLayerJson.Write(layer));

        Assert.Equal(TileId.With(5, TileId.FlipHorizontal), back.GetTile(0, 0));
        Assert.Equal(6, back.GetTile(1, 0));
    }

    // ---- animation ----

    [Fact]
    public void An_animated_tile_shows_the_frame_that_is_current_and_loops()
    {
        var tiles = Set();
        tiles.SetAnimation(5, [new TileFrame(5, 0.5f), new TileFrame(6, 0.25f), new TileFrame(7, 0.25f)]);

        Assert.Equal(5, tiles.ResolveFrame(5, 0.0));
        Assert.Equal(5, tiles.ResolveFrame(5, 0.49));
        Assert.Equal(6, tiles.ResolveFrame(5, 0.5));
        Assert.Equal(7, tiles.ResolveFrame(5, 0.8));
        Assert.Equal(5, tiles.ResolveFrame(5, 1.0));   // wrapped
        Assert.Equal(6, tiles.ResolveFrame(5, 1.6));   // second lap, second frame
    }

    [Fact]
    public void A_plain_tile_is_returned_unchanged()
    {
        var tiles = Set();
        tiles.SetAnimation(5, [new TileFrame(5, 1f), new TileFrame(6, 1f)]);

        Assert.Equal(3, tiles.ResolveFrame(3, 5.0));
        Assert.Equal(0, tiles.ResolveFrame(0, 5.0));
    }

    [Fact]
    public void An_animated_tile_keeps_its_flip_flags_on_every_frame()
    {
        var tiles = Set();
        tiles.SetAnimation(5, [new TileFrame(5, 1f), new TileFrame(6, 1f)]);
        int raw = TileId.With(5, TileId.FlipHorizontal);

        int later = tiles.ResolveFrame(raw, 1.5);

        Assert.Equal(6, TileId.Id(later));
        Assert.Equal(TileId.FlipHorizontal, TileId.Flags(later));
    }

    [Fact]
    public void Negative_time_still_picks_a_valid_frame()
    {
        var tiles = Set();
        tiles.SetAnimation(5, [new TileFrame(5, 1f), new TileFrame(6, 1f)]);

        Assert.Equal(6, tiles.ResolveFrame(5, -0.5)); // half a second before the start is the second half of the loop
    }

    [Fact]
    public void Animation_does_not_change_solidity()
    {
        var tiles = Set();
        tiles.SetSolid(5, true);
        tiles.SetAnimation(5, [new TileFrame(5, 1f), new TileFrame(6, 1f)]);

        Assert.True(tiles.IsSolid(5));
        Assert.False(tiles.IsSolid(6));
        Assert.True(tiles.IsAnimated(5));
        Assert.False(tiles.IsAnimated(6));
    }

    [Fact]
    public void Bad_animations_are_rejected()
    {
        var tiles = Set();

        Assert.Throws<ArgumentException>(() => tiles.SetAnimation(5, []));
        Assert.Throws<ArgumentException>(() => tiles.SetAnimation(5, [new TileFrame(5, 0f)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => tiles.SetAnimation(5, [new TileFrame(99, 1f)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => tiles.SetAnimation(0, [new TileFrame(5, 1f)]));
    }

    [Fact]
    public void Tile_set_files_can_describe_animations()
    {
        var data = TileSetJson.Read("""
            { "texture": "tiles.png", "tileWidth": 16, "tileHeight": 16, "columns": 4, "rows": 2,
              "animations": [ { "tile": 5, "frames": [ { "tile": 5, "duration": 0.5 }, { "tile": 6, "duration": 0.5 } ] } ] }
            """);

        var tiles = data.CreateTileSet(NoTexture, 64, 32);

        Assert.True(tiles.IsAnimated(5));
        Assert.Equal(6, tiles.ResolveFrame(5, 0.75));
    }

    [Fact]
    public void An_animation_without_frames_in_a_file_is_rejected()
    {
        var data = TileSetJson.Read("""
            { "texture": "tiles.png", "tileWidth": 16, "tileHeight": 16, "columns": 4, "rows": 2,
              "animations": [ { "tile": 5, "frames": [] } ] }
            """);

        Assert.Throws<InvalidOperationException>(() => data.CreateTileSet(NoTexture, 64, 32));
    }

    // ---- parallax ----

    [Fact]
    public void With_no_parallax_the_layer_stays_where_it_is()
    {
        var origin = TileLayerRenderSystem.ParallaxOrigin(new Vector3(10, 20, 3), new Vector2(500, 300), Vector2.Zero);

        Assert.Equal(new Vector3(10, 20, 3), origin);
    }

    [Fact]
    public void Full_parallax_follows_the_camera_so_the_layer_is_glued_to_the_screen()
    {
        var origin = TileLayerRenderSystem.ParallaxOrigin(new Vector3(10, 20, 3), new Vector2(500, 300), Vector2.One);

        Assert.Equal(new Vector3(510, 320, 3), origin);
    }

    [Fact]
    public void Half_parallax_moves_the_layer_half_as_far_as_the_camera_and_each_axis_is_independent()
    {
        var origin = TileLayerRenderSystem.ParallaxOrigin(Vector3.Zero, new Vector2(100, 100), new Vector2(0.5f, 0f));

        Assert.Equal(new Vector3(50, 0, 0), origin);
    }
}
