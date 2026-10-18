using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>
/// Component that draws a tile layer. The entity's <c>Transform</c> position is the world position of the layer's cell (0,0)
/// bottom-left corner, and its Z is the draw depth. Rendered by <see cref="TileLayerRenderSystem"/>.
/// </summary>
public struct TileLayerRenderer
{
    /// <summary>The layer to draw.</summary>
    public AssetHandle<TileLayer> Layer;

    /// <summary>The tile set whose atlas supplies the art.</summary>
    public AssetHandle<TileSet> TileSet;

    /// <summary>Tint multiplied with every tile; <see cref="Color.White"/> leaves the art unchanged.</summary>
    public Color Tint;

    /// <summary>
    /// Parallax: how much the layer lags behind the camera, per axis. 0 (the default) moves with the world like any other layer; 0.5 scrolls
    /// at half the camera's speed, so it reads as far away; 1 stays put on the screen (a sky). The layer is shifted by <c>camera position * Parallax</c>.
    /// </summary>
    public System.Numerics.Vector2 Parallax;

    /// <summary>False draws behind sprites (ground, background); true draws in front of them (foreground decoration).</summary>
    public bool Foreground;

    /// <summary>Creates a white-tinted renderer.</summary>
    /// <param name="layer">The layer to draw.</param>
    /// <param name="tileSet">The tile set that supplies the art.</param>
    /// <param name="foreground">True to draw in front of sprites instead of behind them.</param>
    public static TileLayerRenderer Create(AssetHandle<TileLayer> layer, AssetHandle<TileSet> tileSet, bool foreground = false) => new()
    {
        Layer = layer,
        TileSet = tileSet,
        Tint = Color.White,
        Foreground = foreground,
    };
}
