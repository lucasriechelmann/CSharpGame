namespace Engine.Tilemaps;

/// <summary>
/// One layer of tile ids on an unbounded grid, stored as sparse 16x16 chunks. Cell (0,0) is the bottom-left of the layer
/// and Y points up; negative coordinates are allowed. Id 0 means empty; ids 1 and up index a <see cref="TileSet"/>.
/// Empty regions use no memory, and the renderer only visits chunks the camera can see.
/// </summary>
public sealed class TileLayer
{
    /// <summary>Cells per chunk side.</summary>
    public const int ChunkSize = TileChunk.Size;

    private readonly Dictionary<long, TileChunk> _chunks = new();

    /// <summary>Creates an empty layer.</summary>
    /// <param name="tileSize">Side length of one cell in world units.</param>
    public TileLayer(float tileSize)
    {
        if (tileSize <= 0f) throw new ArgumentOutOfRangeException(nameof(tileSize), "Tile size must be positive.");
        TileSize = tileSize;
    }

    /// <summary>Side length of one cell in world units.</summary>
    public float TileSize { get; }

    /// <summary>Number of chunks currently allocated.</summary>
    public int ChunkCount => _chunks.Count;

    /// <summary>Number of non-empty tiles in the layer.</summary>
    public int TileCount => _chunks.Values.Sum(chunk => chunk.Count);

    /// <summary>The chunk index that contains a cell index (floors correctly for negative cells).</summary>
    public static int ChunkIndex(int cell) => cell >> TileChunk.Shift;

    /// <summary>The cell index containing a layer-local coordinate in world units (measured from the layer's origin).</summary>
    public int CellIndex(float local) => (int)MathF.Floor(local / TileSize);

    /// <summary>The tile id at a cell; 0 when empty or when no chunk exists there.</summary>
    public int GetTile(int x, int y) =>
        _chunks.TryGetValue(Key(ChunkIndex(x), ChunkIndex(y)), out var chunk)
            ? chunk.Tiles[LocalIndex(x, y)]
            : 0;

    /// <summary>Places a tile (or clears the cell with id 0). Chunks are created on demand and removed when they become empty.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The id is negative.</exception>
    public void SetTile(int x, int y, int id)
    {
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id), "Tile ids cannot be negative (0 means empty).");

        long key = Key(ChunkIndex(x), ChunkIndex(y));
        if (!_chunks.TryGetValue(key, out var chunk))
        {
            if (id == 0) return; // clearing a cell that has no chunk: nothing to do
            chunk = new TileChunk();
            _chunks[key] = chunk;
        }

        int index = LocalIndex(x, y);
        int previous = chunk.Tiles[index];
        chunk.Tiles[index] = id;

        if (previous == 0 && id != 0) chunk.Count++;
        else if (previous != 0 && id == 0) chunk.Count--;

        if (chunk.Count == 0) _chunks.Remove(key);
    }

    /// <summary>The smallest cell rectangle containing every non-empty tile.</summary>
    /// <returns>False when the layer is empty.</returns>
    public bool TryGetBounds(out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = minY = int.MaxValue;
        maxX = maxY = int.MinValue;

        foreach (var (key, chunk) in _chunks)
        {
            int chunkX = (int)(key >> 32);
            int chunkY = (int)key;
            for (int local = 0; local < chunk.Tiles.Length; local++)
            {
                if (chunk.Tiles[local] == 0) continue;

                int x = (chunkX << TileChunk.Shift) + (local & TileChunk.Mask);
                int y = (chunkY << TileChunk.Shift) + (local >> TileChunk.Shift);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX >= minX;
    }

    /// <summary>
    /// Gives read-only access to one chunk's tiles without copying. Row-major: index = localY * <see cref="ChunkSize"/> + localX,
    /// where the chunk's first cell is (chunkX * ChunkSize, chunkY * ChunkSize).
    /// </summary>
    /// <returns>False when no tile has been placed in that chunk.</returns>
    public bool TryGetChunkTiles(int chunkX, int chunkY, out ReadOnlySpan<int> tiles)
    {
        if (_chunks.TryGetValue(Key(chunkX, chunkY), out var chunk))
        {
            tiles = chunk.Tiles;
            return true;
        }

        tiles = default;
        return false;
    }

    private static long Key(int chunkX, int chunkY) => ((long)chunkX << 32) | (uint)chunkY;

    private static int LocalIndex(int x, int y) => ((y & TileChunk.Mask) << TileChunk.Shift) | (x & TileChunk.Mask);
}
