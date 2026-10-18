namespace Engine.Tilemaps.Tiled;

/// <summary>One frame of a Tiled tile animation.</summary>
/// <param name="LocalId">The tile id inside its tile set (0-based, as Tiled numbers them).</param>
/// <param name="DurationMs">How long the frame lasts, in milliseconds.</param>
public readonly record struct TiledAnimationFrame(int LocalId, int DurationMs);
