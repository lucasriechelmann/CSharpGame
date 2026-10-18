using Engine.Tilemaps;

namespace Engine.Editor.Tiles;

/// <summary>The cell sets the rectangle and fill brushes paint. Pure, so they are unit-tested without a window.</summary>
public static class TileShapes
{
    /// <summary>Every cell of the rectangle between two corner cells, corners included, in any order of the corners.</summary>
    /// <param name="x0">Column of one corner.</param>
    /// <param name="y0">Row of one corner.</param>
    /// <param name="x1">Column of the opposite corner.</param>
    /// <param name="y1">Row of the opposite corner.</param>
    public static IEnumerable<(int X, int Y)> Rectangle(int x0, int y0, int x1, int y1)
    {
        for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
        {
            for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++) yield return (x, y);
        }
    }

    /// <summary>
    /// The cells a bucket fill starting at a cell would change: the connected region (up, down, left and right neighbours) of cells holding the same
    /// raw tile value as the start cell, kept inside a limiting rectangle. An empty region has no natural edge on an unbounded layer, so the limit is
    /// what stops the fill; <paramref name="maxCells"/> is a second guard against an accidental huge fill.
    /// </summary>
    /// <param name="layer">The layer to read.</param>
    /// <param name="startX">Column of the clicked cell.</param>
    /// <param name="startY">Row of the clicked cell.</param>
    /// <param name="minX">Leftmost column the fill may reach.</param>
    /// <param name="minY">Lowest row the fill may reach.</param>
    /// <param name="maxX">Rightmost column the fill may reach.</param>
    /// <param name="maxY">Highest row the fill may reach.</param>
    /// <param name="maxCells">Stop after this many cells.</param>
    /// <returns>The cells to change, starting with the clicked one; empty when the start cell is outside the limit.</returns>
    public static List<(int X, int Y)> FloodFill(TileLayer layer, int startX, int startY, int minX, int minY, int maxX, int maxY, int maxCells = 20000)
    {
        var result = new List<(int X, int Y)>();
        if (startX < minX || startX > maxX || startY < minY || startY > maxY) return result;

        int target = layer.GetTile(startX, startY);
        var seen = new HashSet<(int, int)> { (startX, startY) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));
        while (queue.Count > 0 && result.Count < maxCells)
        {
            var (x, y) = queue.Dequeue();
            result.Add((x, y));
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx < minX || nx > maxX || ny < minY || ny > maxY) continue;
                if (layer.GetTile(nx, ny) != target || !seen.Add((nx, ny))) continue;
                queue.Enqueue((nx, ny));
            }
        }

        return result;
    }
}
