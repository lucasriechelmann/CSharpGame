namespace Engine.Tilemaps.Tiled;

/// <summary>A parsed Tiled map (a <c>.tmj</c> file with its external tile sets resolved): pure data, no engine objects, so it is easy to test.</summary>
/// <param name="Width">Map width in cells (0 for an infinite map, which has no fixed size).</param>
/// <param name="Height">Map height in cells (0 for an infinite map).</param>
/// <param name="TileWidth">Cell width in pixels.</param>
/// <param name="TileHeight">Cell height in pixels.</param>
/// <param name="TileSets">The tile sets, in order of first global id.</param>
/// <param name="TileLayers">The tile layers, bottom first (groups are flattened).</param>
/// <param name="ObjectLayers">The object layers.</param>
/// <param name="Properties">The map custom properties.</param>
public sealed record TiledMap(
    int Width,
    int Height,
    int TileWidth,
    int TileHeight,
    IReadOnlyList<TiledTileSet> TileSets,
    IReadOnlyList<TiledTileLayer> TileLayers,
    IReadOnlyList<TiledObjectLayer> ObjectLayers,
    TiledProperties Properties)
{
    /// <summary>True for an infinite map (stored in chunks, with no fixed size).</summary>
    public bool Infinite => Width == 0 && Height == 0;
}
