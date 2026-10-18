using System.Numerics;
using Engine.Physics2D;

namespace Engine.Tilemaps;

/// <summary>Turns a tile layer into collision: the physics <see cref="TileGrid"/> without Physics2D knowing anything about tilemaps.</summary>
public static class TileGridBuilder
{
    /// <summary>
    /// Builds a grid covering the layer's used area, marking a cell solid when its tile is solid in the tile set.
    /// </summary>
    /// <param name="layer">The layer to read.</param>
    /// <param name="tileSet">Says which tile ids are solid.</param>
    /// <param name="layerOrigin">World position of the layer's cell (0,0) bottom-left corner (normally the layer entity's position).</param>
    /// <exception cref="InvalidOperationException">The layer has no tiles.</exception>
    public static TileGrid FromLayer(TileLayer layer, TileSet tileSet, Vector2 layerOrigin)
    {
        if (!layer.TryGetBounds(out int minX, out int minY, out int maxX, out int maxY))
            throw new InvalidOperationException("Cannot build a collision grid from an empty tile layer.");

        var origin = layerOrigin + new Vector2(minX, minY) * layer.TileSize;
        var grid = new TileGrid(maxX - minX + 1, maxY - minY + 1, layer.TileSize, origin);

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                if (tileSet.IsSolid(layer.GetTile(x, y)))
                    grid.SetSolid(x - minX, y - minY, true);

        return grid;
    }
}
