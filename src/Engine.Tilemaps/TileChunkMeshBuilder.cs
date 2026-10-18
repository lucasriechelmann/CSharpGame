using System.Numerics;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>
/// Turns one chunk of a tile layer into the quads of its cached mesh. Pure CPU work with no GL, so it is unit-tested. Positions are in the layer's own space
/// (cell (0, 0) at the origin), white and opaque: the layer's position (parallax included) and tint are applied when the mesh is drawn, so scrolling or
/// fading a layer never rebuilds it.
/// </summary>
internal static class TileChunkMeshBuilder
{
    /// <summary>
    /// How far, in atlas pixels, a tile's texture rectangle is pulled in on every side, so linear sampling and rounding never pick up a neighbouring
    /// tile's colour at the edge of a tile.
    /// </summary>
    public const float AtlasInsetPixels = 0.02f;

    /// <summary>Vertices per quad.</summary>
    public const int VerticesPerQuad = SpriteQuad.VertexCount;

    /// <summary>Builds the quads for every static tile of a chunk and lists the animated ones separately.</summary>
    /// <param name="tiles">The chunk's tile values, row-major (see <see cref="TileLayer.TryGetChunk"/>).</param>
    /// <param name="chunkX">The chunk's column.</param>
    /// <param name="chunkY">The chunk's row.</param>
    /// <param name="tileSize">Size of one cell in world units.</param>
    /// <param name="tileSet">The tile set that gives each tile's picture and says which are animated.</param>
    /// <param name="textureWidth">Atlas width in pixels.</param>
    /// <param name="textureHeight">Atlas height in pixels.</param>
    /// <param name="vertices">Receives <see cref="VerticesPerQuad"/> vertices per static tile; cleared first.</param>
    /// <param name="animated">Receives the animated tiles; cleared first.</param>
    /// <returns>The number of quads written.</returns>
    public static int Build(ReadOnlySpan<int> tiles, int chunkX, int chunkY, float tileSize, TileSet tileSet,
        int textureWidth, int textureHeight, List<SpriteVertex> vertices, List<AnimatedCell> animated)
    {
        vertices.Clear();
        animated.Clear();

        int firstX = chunkX * TileLayer.ChunkSize;
        int firstY = chunkY * TileLayer.ChunkSize;
        Span<SpriteVertex> quad = stackalloc SpriteVertex[VerticesPerQuad];
        int quads = 0;
        for (int local = 0; local < tiles.Length; local++)
        {
            int raw = tiles[local];
            if (raw == 0) continue;

            int cellX = firstX + local % TileLayer.ChunkSize;
            int cellY = firstY + local / TileLayer.ChunkSize;
            if (tileSet.IsAnimated(raw))
            {
                animated.Add(new AnimatedCell(cellX, cellY, raw));
                continue;
            }

            SpriteQuad.Write(new SpriteDraw
            {
                Position = new Vector3((cellX + 0.5f) * tileSize, (cellY + 0.5f) * tileSize, 0f),
                Size = new Vector2(tileSize, tileSize),
                Pivot = new Vector2(0.5f, 0.5f),
                UvRect = tileSet.GetSourceRect(raw).Inset(AtlasInsetPixels).ToUv(textureWidth, textureHeight),
                Flip = FlipOf(raw),
                Color = Color.White,
            }, quad);
            for (int i = 0; i < VerticesPerQuad; i++) vertices.Add(quad[i]);
            quads++;
        }

        return quads;
    }

    /// <summary>The sprite flip a raw tile value asks for.</summary>
    /// <param name="raw">A stored tile value.</param>
    public static SpriteFlip FlipOf(int raw)
    {
        var flip = SpriteFlip.None;
        if ((raw & TileId.FlipHorizontal) != 0) flip |= SpriteFlip.Horizontal;
        if ((raw & TileId.FlipVertical) != 0) flip |= SpriteFlip.Vertical;
        if ((raw & TileId.FlipDiagonal) != 0) flip |= SpriteFlip.Diagonal;
        return flip;
    }
}
