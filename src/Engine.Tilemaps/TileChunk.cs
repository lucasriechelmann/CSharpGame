namespace Engine.Tilemaps;

/// <summary>A fixed 16x16 block of tile ids inside a <see cref="TileLayer"/>. Chunks exist only where tiles have been placed.</summary>
internal sealed class TileChunk
{
    /// <summary>Cells per chunk side. A power of two so cell-to-chunk conversion is a shift and a mask.</summary>
    public const int Size = 16;

    /// <summary>Bit shift that converts a cell index to a chunk index (log2 of <see cref="Size"/>).</summary>
    public const int Shift = 4;

    /// <summary>Mask that converts a cell index to its index inside the chunk.</summary>
    public const int Mask = Size - 1;

    /// <summary>Tile ids in row-major order: index = localY * Size + localX. Zero means empty.</summary>
    public readonly int[] Tiles = new int[Size * Size];

    /// <summary>Number of non-empty tiles; when it drops to zero the chunk is discarded.</summary>
    public int Count;
}
