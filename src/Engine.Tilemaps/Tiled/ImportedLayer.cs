using System.Numerics;

namespace Engine.Tilemaps.Tiled;

/// <summary>A tile layer converted to the engine's layer, with how to place and draw it.</summary>
/// <param name="Name">The layer name (a layer that uses several tile sets is split in one per tile set, named <c>layer#tileset</c>).</param>
/// <param name="Layer">The tiles, with Y pointing up and flip flags in <see cref="TileId"/> form.</param>
/// <param name="TileSetName">Which <see cref="ImportedTileSet"/> supplies the art.</param>
/// <param name="Position">World position of the layer cell (0,0): the layer offset, flipped to Y up.</param>
/// <param name="Parallax">The engine parallax factor (0 moves with the world, 1 stays on screen), converted from Tiled where 1 is normal.</param>
/// <param name="Foreground">True when the layer has a boolean <c>foreground</c> property: draw it in front of sprites.</param>
/// <param name="Visible">The layer visibility flag in Tiled (hidden layers are usually helpers: skip them).</param>
/// <param name="Opacity">0 to 1, used as the tint alpha.</param>
/// <param name="Order">The layer index in the map, bottom first, for assigning draw depth.</param>
public sealed record ImportedLayer(
    string Name,
    TileLayer Layer,
    string TileSetName,
    Vector2 Position,
    Vector2 Parallax,
    bool Foreground,
    bool Visible,
    float Opacity,
    int Order);
