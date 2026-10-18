namespace Engine.Tilemaps.Tiled;

/// <summary>A Tiled tile set: an image cut into equal tiles (collections of separate images are not supported).</summary>
/// <param name="Name">The tile set name.</param>
/// <param name="FirstGid">The global id of its first tile; a map tile value from here on belongs to it, up to the next tile set.</param>
/// <param name="TileWidth">Tile width in pixels.</param>
/// <param name="TileHeight">Tile height in pixels.</param>
/// <param name="Spacing">Pixels between tiles in the image.</param>
/// <param name="Margin">Pixels around the grid of tiles in the image.</param>
/// <param name="Columns">Tiles per row.</param>
/// <param name="TileCount">Total number of tiles.</param>
/// <param name="Image">The image path as written in the file, relative to the file that names it.</param>
/// <param name="ImageWidth">The image width in pixels.</param>
/// <param name="ImageHeight">The image height in pixels.</param>
/// <param name="Tiles">Per-tile extras (animations, properties) for the tiles that have any.</param>
public sealed record TiledTileSet(
    string Name,
    int FirstGid,
    int TileWidth,
    int TileHeight,
    int Spacing,
    int Margin,
    int Columns,
    int TileCount,
    string Image,
    int ImageWidth,
    int ImageHeight,
    IReadOnlyList<TiledTile> Tiles);
