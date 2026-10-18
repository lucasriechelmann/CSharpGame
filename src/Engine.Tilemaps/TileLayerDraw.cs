using System.Numerics;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>A tile layer gathered from the world, waiting to be sorted by depth and drawn.</summary>
internal struct TileLayerDraw
{
    /// <summary>World position of the layer's cell (0,0) bottom-left corner.</summary>
    public Vector3 Origin;

    /// <summary>The tile data.</summary>
    public TileLayer Layer;

    /// <summary>Atlas layout and solidity.</summary>
    public TileSet TileSet;

    /// <summary>The resolved atlas texture.</summary>
    public Texture2D Texture;

    /// <summary>Tint applied to every tile.</summary>
    public Color Tint;
}
