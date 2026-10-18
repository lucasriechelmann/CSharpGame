using System.Numerics;
using Engine.Physics2D;

namespace Platformer;

/// <summary>
/// The demo level, defined as rows of solid tiles. Row 0 is the top. It builds the collision <see cref="TileGrid"/>;
/// the tile sprites are spawned separately by the game.
/// </summary>
public static class LevelLayout
{
    /// <summary>Number of tile columns.</summary>
    public const int Columns = 40;

    /// <summary>Side length of one tile in world units.</summary>
    public const float TileSize = 32f;

    // Each row is walls at both ends plus the listed (start column, length) runs of solid tiles.
    private static readonly string[] Rows =
    [
        FullRow(),
        Row(), Row(), Row(),
        Row((17, 5)),
        Row(),
        Row((6, 4), (30, 4)),
        Row(),
        Row((14, 6)),
        Row(),
        Row((3, 3), (26, 5)),
        Row(),
        Row((10, 5), (34, 3)),
        Row(),
        Row((20, 4)),
        Row(), Row(),
        FullRow(),
    ];

    /// <summary>Number of tile rows.</summary>
    public static int RowCount => Rows.Length;

    /// <summary>World position of the bottom-left corner of the level, chosen so the level is centred on the origin.</summary>
    public static Vector2 Origin => new(-Columns * TileSize / 2f, -RowCount * TileSize / 2f);

    /// <summary>Builds the collision grid. Grid row 0 is the bottom, so the layout is flipped vertically.</summary>
    public static TileGrid CreateGrid()
    {
        var grid = new TileGrid(Columns, RowCount, TileSize, Origin);
        for (int row = 0; row < RowCount; row++)
            for (int column = 0; column < Columns; column++)
                if (Rows[row][column] == '#')
                    grid.SetSolid(column, RowCount - 1 - row, true);

        return grid;
    }

    private static string FullRow() => new('#', Columns);

    private static string Row(params (int Start, int Length)[] solids)
    {
        var cells = new char[Columns];
        Array.Fill(cells, '.');
        cells[0] = '#';
        cells[^1] = '#';
        foreach (var (start, length) in solids)
            for (int i = start; i < start + length; i++)
                cells[i] = '#';
        return new string(cells);
    }
}
