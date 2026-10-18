namespace Engine.Tilemaps.Tiled;

/// <summary>A Tiled object layer.</summary>
/// <param name="Name">The layer name.</param>
/// <param name="OffsetX">Horizontal offset in pixels.</param>
/// <param name="OffsetY">Vertical offset in pixels (Tiled Y points down).</param>
/// <param name="Objects">The objects, in file order.</param>
public sealed record TiledObjectLayer(string Name, float OffsetX, float OffsetY, IReadOnlyList<TiledObject> Objects);
