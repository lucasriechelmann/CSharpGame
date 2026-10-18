namespace Engine.Tilemaps.Tiled;

/// <summary>One non-empty cell of a Tiled tile layer.</summary>
/// <param name="X">Column, counted from the map left edge (negative is possible in an infinite map).</param>
/// <param name="Y">Row, counted from the map <b>top</b> edge downwards, as Tiled does.</param>
/// <param name="Gid">The global tile id with the flip bits still in it (bits 31, 30 and 29).</param>
public readonly record struct TiledCell(int X, int Y, uint Gid);
