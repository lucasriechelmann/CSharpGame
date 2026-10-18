using System.Numerics;

namespace Engine.Runtime.Editor.Tiles;

/// <summary>Cell arithmetic for the tile painter, as pure functions.</summary>
public static class TileCells
{
    /// <summary>The cell of a tile layer that contains a world point.</summary>
    /// <param name="world">The point in world space.</param>
    /// <param name="layerOrigin">World position of the layer's cell (0, 0) corner, which is the layer entity's position.</param>
    /// <param name="tileSize">World units per cell.</param>
    public static (int X, int Y) FromWorld(Vector2 world, Vector2 layerOrigin, float tileSize)
    {
        if (tileSize <= 0f) throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Tile size must be positive.");

        var local = (world - layerOrigin) / tileSize;
        return ((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y));
    }

    /// <summary>The world-space rectangle of a cell: the corner nearest the layer origin and the opposite corner.</summary>
    /// <param name="x">Cell column.</param>
    /// <param name="y">Cell row.</param>
    /// <param name="layerOrigin">World position of the layer's cell (0, 0) corner.</param>
    /// <param name="tileSize">World units per cell.</param>
    public static (Vector2 Min, Vector2 Max) Bounds(int x, int y, Vector2 layerOrigin, float tileSize)
    {
        var min = layerOrigin + new Vector2(x, y) * tileSize;
        return (min, min + new Vector2(tileSize));
    }

    /// <summary>
    /// Every cell on the straight line from one cell to another, both ends included (Bresenham). A fast mouse drag skips cells between
    /// frames; painting along this line leaves no gaps.
    /// </summary>
    /// <param name="fromX">Start cell column.</param>
    /// <param name="fromY">Start cell row.</param>
    /// <param name="toX">End cell column.</param>
    /// <param name="toY">End cell row.</param>
    public static IEnumerable<(int X, int Y)> Line(int fromX, int fromY, int toX, int toY)
    {
        int dx = Math.Abs(toX - fromX);
        int dy = -Math.Abs(toY - fromY);
        int stepX = fromX < toX ? 1 : -1;
        int stepY = fromY < toY ? 1 : -1;
        int error = dx + dy;

        int x = fromX;
        int y = fromY;
        while (true)
        {
            yield return (x, y);
            if (x == toX && y == toY) yield break;

            int doubled = 2 * error;
            if (doubled >= dy)
            {
                error += dy;
                x += stepX;
            }

            if (doubled <= dx)
            {
                error += dx;
                y += stepY;
            }
        }
    }
}
