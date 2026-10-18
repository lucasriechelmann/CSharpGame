namespace Engine.Tilemaps.Tiled;

/// <summary>A Tiled tile layer, reduced to its non-empty cells.</summary>
/// <param name="Name">The layer name.</param>
/// <param name="Visible">Whether the layer is shown in the editor (hidden layers are usually helpers).</param>
/// <param name="Opacity">0 to 1.</param>
/// <param name="OffsetX">Horizontal offset in pixels (right is positive).</param>
/// <param name="OffsetY">Vertical offset in pixels (Tiled Y points <b>down</b>).</param>
/// <param name="ParallaxX">Tiled horizontal parallax factor: 1 moves with the camera like normal, 0 stays on the screen.</param>
/// <param name="ParallaxY">Tiled vertical parallax factor.</param>
/// <param name="Properties">Custom properties (a boolean <c>foreground</c> draws the layer in front of sprites).</param>
/// <param name="Cells">The non-empty cells.</param>
public sealed record TiledTileLayer(
    string Name,
    bool Visible,
    float Opacity,
    float OffsetX,
    float OffsetY,
    float ParallaxX,
    float ParallaxY,
    TiledProperties Properties,
    IReadOnlyList<TiledCell> Cells);
