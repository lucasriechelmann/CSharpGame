using Engine.Core.Assets;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Engine.Tilemaps.Tests;

public class TileChunkMeshTests
{
    private static TileSet NewTileSet()
    {
        var tiles = new TileSet(AssetHandle<Texture2D>.Invalid, 16, 16, columns: 4, rows: 1);
        tiles.SetAnimation(3, [new TileFrame(3, 0.5f), new TileFrame(4, 0.5f)]);
        return tiles;
    }

    private static int[] Chunk(params (int Index, int Value)[] cells)
    {
        var tiles = new int[TileLayer.ChunkSize * TileLayer.ChunkSize];
        foreach (var (index, value) in cells) tiles[index] = value;
        return tiles;
    }

    [Fact]
    public void Static_tiles_become_quads_in_layer_space_and_animated_ones_are_listed_instead()
    {
        var vertices = new List<SpriteVertex>();
        var animated = new List<AnimatedCell>();
        int[] tiles = Chunk((0, 1), (1, 2), (2, 3)); // cells (0,0), (1,0) and (2,0); tile 3 is animated

        int quads = TileChunkMeshBuilder.Build(tiles, 0, 0, 32f, NewTileSet(), 64, 16, vertices, animated);

        Assert.Equal(2, quads);
        Assert.Equal(2 * TileChunkMeshBuilder.VerticesPerQuad, vertices.Count);
        Assert.Equal(new AnimatedCell(2, 0, 3), Assert.Single(animated));
        // The first tile fills cell (0,0): x 0..32, y 0..32.
        Assert.Equal(0f, vertices.Take(4).Min(v => v.Position.X), 3);
        Assert.Equal(32f, vertices.Take(4).Max(v => v.Position.X), 3);
        Assert.Equal(64f, vertices.Skip(4).Take(4).Max(v => v.Position.X), 3); // the second tile fills cell (1,0): x 32..64
    }

    [Fact]
    public void A_chunk_away_from_the_origin_is_offset_by_whole_chunks()
    {
        var vertices = new List<SpriteVertex>();
        var animated = new List<AnimatedCell>();

        TileChunkMeshBuilder.Build(Chunk((0, 1)), chunkX: 2, chunkY: -1, tileSize: 10f, NewTileSet(), 64, 16, vertices, animated);

        // Cell (32, -16) starts at world (320, -160).
        Assert.Equal(320f, vertices.Min(v => v.Position.X), 3);
        Assert.Equal(-160f, vertices.Min(v => v.Position.Y), 3);
    }

    [Fact]
    public void Quads_are_white_so_the_layer_tint_can_be_applied_when_drawing()
    {
        var vertices = new List<SpriteVertex>();

        TileChunkMeshBuilder.Build(Chunk((5, 1)), 0, 0, 16f, NewTileSet(), 64, 16, vertices, new List<AnimatedCell>());

        Assert.All(vertices, v => Assert.Equal(Color.White, v.Color));
    }

    [Fact]
    public void Flip_flags_change_the_texture_coordinates_but_not_the_positions()
    {
        var plain = new List<SpriteVertex>();
        var flipped = new List<SpriteVertex>();

        TileChunkMeshBuilder.Build(Chunk((0, 1)), 0, 0, 16f, NewTileSet(), 64, 16, plain, new List<AnimatedCell>());
        TileChunkMeshBuilder.Build(Chunk((0, TileId.With(1, TileId.FlipHorizontal))), 0, 0, 16f, NewTileSet(), 64, 16, flipped, new List<AnimatedCell>());

        Assert.Equal(plain.Select(v => v.Position), flipped.Select(v => v.Position));
        Assert.NotEqual(plain.Select(v => v.TexCoord), flipped.Select(v => v.TexCoord));
        Assert.Equal(SpriteFlip.Horizontal | SpriteFlip.Vertical, TileChunkMeshBuilder.FlipOf(TileId.With(1, TileId.FlipHorizontal | TileId.FlipVertical)));
    }

    [Fact]
    public void An_empty_chunk_builds_nothing()
    {
        var vertices = new List<SpriteVertex>();

        int quads = TileChunkMeshBuilder.Build(Chunk(), 0, 0, 16f, NewTileSet(), 64, 16, vertices, new List<AnimatedCell>());

        Assert.Equal(0, quads);
        Assert.Empty(vertices);
    }

    [Fact]
    public void A_chunk_version_changes_only_when_a_tile_really_changes()
    {
        var layer = new TileLayer(16f);
        layer.SetTile(1, 1, 5);
        layer.TryGetChunk(0, 0, out _, out int first);

        layer.SetTile(1, 1, 5); // the same value again
        layer.TryGetChunk(0, 0, out _, out int same);
        layer.SetTile(2, 1, 6);
        layer.TryGetChunk(0, 0, out _, out int changed);

        Assert.Equal(first, same);
        Assert.True(changed > first);
        Assert.False(layer.TryGetChunk(9, 9, out _, out _));
    }

    [Fact]
    public void A_tile_set_version_changes_when_an_animation_is_added()
    {
        var tiles = new TileSet(AssetHandle<Texture2D>.Invalid, 16, 16, columns: 4, rows: 1);
        int before = tiles.Version;

        tiles.SetAnimation(1, [new TileFrame(1, 1f), new TileFrame(2, 1f)]);

        Assert.True(tiles.Version > before);
    }
}
