using System.Numerics;

namespace Engine.Physics2D;

/// <summary>
/// A grid of solid/empty cells used for tile collision. Cell (0,0) is the bottom-left; world Y points up.
/// Anything outside the grid counts as empty. It stores collision only; the tile art lives elsewhere.
/// </summary>
public sealed class TileGrid
{
    private readonly bool[] _solid;

    /// <summary>Creates an all-empty grid.</summary>
    /// <param name="width">Number of columns.</param>
    /// <param name="height">Number of rows.</param>
    /// <param name="tileSize">Side length of a cell in world units.</param>
    /// <param name="origin">World position of the bottom-left corner of cell (0,0).</param>
    public TileGrid(int width, int height, float tileSize, Vector2 origin)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (tileSize <= 0f) throw new ArgumentOutOfRangeException(nameof(tileSize));

        Width = width;
        Height = height;
        TileSize = tileSize;
        Origin = origin;
        _solid = new bool[width * height];
    }

    /// <summary>Number of columns.</summary>
    public int Width { get; }

    /// <summary>Number of rows.</summary>
    public int Height { get; }

    /// <summary>Side length of a cell in world units.</summary>
    public float TileSize { get; }

    /// <summary>World position of the bottom-left corner of cell (0,0).</summary>
    public Vector2 Origin { get; }

    /// <summary>True when the cell is solid. Cells outside the grid are empty.</summary>
    public bool IsSolid(int x, int y) => InRange(x, y) && _solid[y * Width + x];

    /// <summary>Marks a cell solid or empty.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public void SetSolid(int x, int y, bool solid)
    {
        if (!InRange(x, y))
            throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x},{y}) is outside the {Width}x{Height} grid.");
        _solid[y * Width + x] = solid;
    }

    /// <summary>The cell column containing a world X coordinate (may be outside the grid, including negative).</summary>
    public int CellX(float worldX) => (int)MathF.Floor((worldX - Origin.X) / TileSize);

    /// <summary>The cell row containing a world Y coordinate (may be outside the grid, including negative).</summary>
    public int CellY(float worldY) => (int)MathF.Floor((worldY - Origin.Y) / TileSize);

    /// <summary>The cell index containing a world coordinate along the given axis.</summary>
    public int CellIndex(float world, Axis axis) => axis == Axis.X ? CellX(world) : CellY(world);

    /// <summary>World coordinate of the grid's lower edge along an axis.</summary>
    public float OriginAlong(Axis axis) => axis == Axis.X ? Origin.X : Origin.Y;

    /// <summary>Number of cells along an axis.</summary>
    public int CellCount(Axis axis) => axis == Axis.X ? Width : Height;

    /// <summary>The world-space box of a cell.</summary>
    public Aabb CellBounds(int x, int y)
    {
        var min = new Vector2(Origin.X + x * TileSize, Origin.Y + y * TileSize);
        return new Aabb(min, min + new Vector2(TileSize));
    }

    /// <summary>The world-space centre of a cell.</summary>
    public Vector2 CellCenter(int x, int y) => CellBounds(x, y).Center;

    private bool InRange(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
}
