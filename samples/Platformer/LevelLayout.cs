using System.Numerics;
using Engine.Core.Assets;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Platformer;

/// <summary>
/// The demo level, defined as rows of solid tiles (row 0 is the top). It builds the three tile layers (background,
/// ground, foreground) and the tile set; collision comes from the ground layer via <see cref="TileGridBuilder"/>.
/// </summary>
public static class LevelLayout
{
    /// <summary>Number of tile columns.</summary>
    public const int Columns = 40;

    /// <summary>Side length of one tile in world units (the 16-pixel art is drawn at 2x).</summary>
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

    /// <summary>World position of the layers' cell (0,0) bottom-left corner, chosen so the level is centred on the origin.</summary>
    public static Vector2 Origin => new(-Columns * TileSize / 2f, -RowCount * TileSize / 2f);

    /// <summary>Creates the tile set for <c>tiles.png</c> and marks the solid tiles.</summary>
    public static TileSet CreateTileSet(AssetManager assets, AssetHandle<Texture2D> atlas)
    {
        var tileSet = TileSet.FromTexture(assets, atlas, tileWidth: 16, tileHeight: 16);
        tileSet.SetSolid(TileIds.Dirt, true);
        tileSet.SetSolid(TileIds.Grass, true);
        tileSet.SetSolid(TileIds.Stone, true);
        return tileSet;
    }

    /// <summary>The solid terrain. Walls and ceiling are stone; a solid tile with air above gets a grass cap, otherwise dirt.</summary>
    public static TileLayer CreateGround()
    {
        var layer = new TileLayer(TileSize);
        for (int row = 0; row < RowCount; row++)
            for (int column = 0; column < Columns; column++)
                if (IsSolid(row, column))
                    layer.SetTile(column, GridY(row), GroundTile(row, column));

        return layer;
    }

    /// <summary>Background bricks behind the open interior of the level.</summary>
    public static TileLayer CreateBackground()
    {
        var layer = new TileLayer(TileSize);
        for (int row = 1; row < RowCount - 1; row++)
            for (int column = 1; column < Columns - 1; column++)
                if (!IsSolid(row, column))
                    layer.SetTile(column, GridY(row), TileIds.Brick);

        return layer;
    }

    /// <summary>Grass tufts standing on some grass-topped tiles, drawn in front of characters.</summary>
    public static TileLayer CreateForeground()
    {
        var layer = new TileLayer(TileSize);
        for (int row = 1; row < RowCount; row++)
            for (int column = 1; column < Columns - 1; column++)
                if (column % 4 == 1 && IsSolid(row, column) && !IsSolid(row - 1, column) && GroundTile(row, column) == TileIds.Grass)
                    layer.SetTile(column, GridY(row - 1), TileIds.Tuft);

        return layer;
    }

    private static bool IsSolid(int row, int column) => Rows[row][column] == '#';

    /// <summary>Layer rows count upwards from the bottom, so the layout is flipped vertically.</summary>
    private static int GridY(int row) => RowCount - 1 - row;

    private static int GroundTile(int row, int column)
    {
        if (row == 0 || column == 0 || column == Columns - 1) return TileIds.Stone;
        return IsSolid(row - 1, column) ? TileIds.Dirt : TileIds.Grass;
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
