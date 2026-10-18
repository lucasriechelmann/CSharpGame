namespace Engine.Tilemaps;

/// <summary>
/// The layout of a raw tile value stored in a <see cref="TileLayer"/>: the low 28 bits are the tile id (0 empty, 1 and up index a
/// <see cref="TileSet"/>) and the next three bits say how the tile is flipped when drawn. The same three flags as Tiled uses, moved down so
/// a raw value is never negative.
/// </summary>
public static class TileId
{
    /// <summary>Mirror the tile left to right.</summary>
    public const int FlipHorizontal = 1 << 28;

    /// <summary>Mirror the tile top to bottom.</summary>
    public const int FlipVertical = 1 << 29;

    /// <summary>Swap the tile's x and y axes (a mirror along its diagonal). Combined with the other two it gives the four rotations; applied first, like Tiled.</summary>
    public const int FlipDiagonal = 1 << 30;

    /// <summary>All three flag bits.</summary>
    public const int FlagMask = FlipHorizontal | FlipVertical | FlipDiagonal;

    /// <summary>The largest tile id a raw value can carry.</summary>
    public const int MaxId = FlipHorizontal - 1;

    /// <summary>The tile id of a raw value, with the flip flags removed.</summary>
    /// <param name="raw">A value from <see cref="TileLayer.GetTile"/>.</param>
    public static int Id(int raw) => raw & ~FlagMask;

    /// <summary>The flip flags of a raw value (a combination of the <c>Flip*</c> constants).</summary>
    /// <param name="raw">A value from <see cref="TileLayer.GetTile"/>.</param>
    public static int Flags(int raw) => raw & FlagMask;

    /// <summary>Combines a tile id with flip flags into a raw value.</summary>
    /// <param name="id">The tile id (1 to <see cref="MaxId"/>, or 0 for empty).</param>
    /// <param name="flags">A combination of the <c>Flip*</c> constants.</param>
    /// <exception cref="ArgumentOutOfRangeException">The id is negative or too large, or flags contains other bits.</exception>
    public static int With(int id, int flags = 0)
    {
        if (id < 0 || id > MaxId) throw new ArgumentOutOfRangeException(nameof(id), $"A tile id is 0 to {MaxId}, got {id}.");
        if ((flags & ~FlagMask) != 0) throw new ArgumentOutOfRangeException(nameof(flags), $"Flags may only use the Flip* bits, got {flags}.");
        return id | flags;
    }

    /// <summary>Converts the flip bits of a Tiled global tile id (bits 31, 30, 29 for horizontal, vertical, diagonal) into this class's flags.</summary>
    /// <param name="tiledGid">The raw 32-bit global id from a Tiled map.</param>
    public static int FlagsFromTiled(uint tiledGid)
    {
        int flags = 0;
        if ((tiledGid & 0x80000000u) != 0) flags |= FlipHorizontal;
        if ((tiledGid & 0x40000000u) != 0) flags |= FlipVertical;
        if ((tiledGid & 0x20000000u) != 0) flags |= FlipDiagonal;
        return flags;
    }

    /// <summary>The tile id of a Tiled global id, with Tiled's flip bits (and the unused hexagonal rotation bit) removed.</summary>
    /// <param name="tiledGid">The raw 32-bit global id from a Tiled map.</param>
    public static uint IdFromTiled(uint tiledGid) => tiledGid & 0x0FFFFFFFu;
}
