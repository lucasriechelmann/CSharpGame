namespace Engine.Tilemaps;

/// <summary>One frame of an animated tile: which tile to show and for how long.</summary>
/// <param name="TileId">The tile id to draw during this frame (1-based, an index into the same tile set).</param>
/// <param name="Duration">How long the frame lasts, in seconds (positive).</param>
public readonly record struct TileFrame(int TileId, float Duration);
