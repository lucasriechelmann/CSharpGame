namespace Engine.Runtime.Editing;

/// <summary>One cell of a tile layer that changed: where, and the id before and after.</summary>
/// <param name="X">Cell column.</param>
/// <param name="Y">Cell row.</param>
/// <param name="Before">Tile id before the change (0 = empty).</param>
/// <param name="After">Tile id after the change.</param>
public readonly record struct TileChange(int X, int Y, int Before, int After);
