namespace Engine.Tilemaps.Tiled;

/// <summary>A Tiled tile set converted to the engine's tile set description, waiting for its texture to be loaded.</summary>
/// <param name="Name">A unique name for the tile set within the map.</param>
/// <param name="TextureName">The asset name of the atlas texture.</param>
/// <param name="Data">The tile set description (tile size, solid tiles, animations); turn it into a <see cref="TileSet"/> with <see cref="TileSetData.CreateTileSet"/>.</param>
public sealed record ImportedTileSet(string Name, string TextureName, TileSetData Data);
