namespace Engine.Tilemaps.Tiled;

/// <summary>How a Tiled map is turned into engine data.</summary>
public sealed class TiledImportOptions
{
    /// <summary>
    /// World units per map pixel. The art may be drawn bigger than it was authored: the Platformer draws its 16 pixel tiles 32 units wide, so
    /// it imports with 2. Every position, size and tile size in the result is scaled by this.
    /// </summary>
    public float WorldUnitsPerPixel { get; set; } = 1f;

    /// <summary>
    /// A folder (relative to the asset root, with a trailing slash) put in front of each tile set image's file name to get its texture asset
    /// name: an image <c>../art/tiles.png</c> in the map becomes <c>maps/tiles.png</c> with <c>"maps/"</c>. Only the file name of the image is kept,
    /// so copy the images to that folder.
    /// </summary>
    public string TextureFolder { get; set; } = "";
}
