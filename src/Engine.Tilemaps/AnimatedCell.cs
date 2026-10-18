namespace Engine.Tilemaps;

/// <summary>An animated tile found while building a chunk's cached mesh: it is left out of the mesh and drawn every frame instead, because its picture changes.</summary>
/// <param name="CellX">Cell column in the layer.</param>
/// <param name="CellY">Cell row in the layer.</param>
/// <param name="Raw">The stored tile value, flip flags included.</param>
internal readonly record struct AnimatedCell(int CellX, int CellY, int Raw);
