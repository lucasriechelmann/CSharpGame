using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>
/// A texture atlas cut into equal tiles, plus per-tile properties (currently: solid or not). Tile ids are 1-based and run
/// left to right, top to bottom, so id 1 is the top-left tile; id 0 is reserved for "empty".
/// </summary>
public sealed class TileSet
{
    private readonly bool[] _solid;

    /// <summary>Describes an atlas whose size is already known.</summary>
    /// <param name="texture">Handle of the atlas texture.</param>
    /// <param name="tileWidth">Width of one tile in pixels.</param>
    /// <param name="tileHeight">Height of one tile in pixels.</param>
    /// <param name="columns">Number of tiles per row.</param>
    /// <param name="rows">Number of rows of tiles.</param>
    /// <param name="spacing">Pixels between neighbouring tiles.</param>
    /// <param name="margin">Pixels around the whole grid of tiles.</param>
    public TileSet(AssetHandle<Texture2D> texture, int tileWidth, int tileHeight, int columns, int rows, int spacing = 0, int margin = 0)
    {
        if (tileWidth <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (spacing < 0) throw new ArgumentOutOfRangeException(nameof(spacing));
        if (margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));

        Texture = texture;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Columns = columns;
        Rows = rows;
        Spacing = spacing;
        Margin = margin;
        _solid = new bool[TileCount + 1];
    }

    /// <summary>Handle of the atlas texture.</summary>
    public AssetHandle<Texture2D> Texture { get; }

    /// <summary>Width of one tile in pixels.</summary>
    public int TileWidth { get; }

    /// <summary>Height of one tile in pixels.</summary>
    public int TileHeight { get; }

    /// <summary>Number of tiles per row.</summary>
    public int Columns { get; }

    /// <summary>Number of rows of tiles.</summary>
    public int Rows { get; }

    /// <summary>Pixels between neighbouring tiles.</summary>
    public int Spacing { get; }

    /// <summary>Pixels around the whole grid of tiles.</summary>
    public int Margin { get; }

    /// <summary>Total number of tiles in the atlas (valid ids are 1 to this value).</summary>
    public int TileCount => Columns * Rows;

    /// <summary>Works out the column and row counts from the loaded texture's size.</summary>
    /// <param name="assets">Manager that owns the texture.</param>
    /// <param name="texture">Handle of the atlas texture.</param>
    /// <param name="tileWidth">Width of one tile in pixels.</param>
    /// <param name="tileHeight">Height of one tile in pixels.</param>
    /// <param name="spacing">Pixels between neighbouring tiles.</param>
    /// <param name="margin">Pixels around the whole grid of tiles.</param>
    public static TileSet FromTexture(AssetManager assets, AssetHandle<Texture2D> texture, int tileWidth, int tileHeight, int spacing = 0, int margin = 0)
    {
        var atlas = assets.Get(texture);
        int columns = (atlas.Width - 2 * margin + spacing) / (tileWidth + spacing);
        int rows = (atlas.Height - 2 * margin + spacing) / (tileHeight + spacing);
        return new TileSet(texture, tileWidth, tileHeight, columns, rows, spacing, margin);
    }

    /// <summary>The tile's rectangle in atlas pixels (top-left origin).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The id is 0 or larger than <see cref="TileCount"/>.</exception>
    public Rect GetSourceRect(int tileId)
    {
        if (tileId < 1 || tileId > TileCount)
            throw new ArgumentOutOfRangeException(nameof(tileId), $"Tile id {tileId} is outside 1..{TileCount}.");

        int index = tileId - 1;
        int column = index % Columns;
        int row = index / Columns;
        return new Rect(
            Margin + column * (TileWidth + Spacing),
            Margin + row * (TileHeight + Spacing),
            TileWidth,
            TileHeight);
    }

    /// <summary>True when the tile blocks movement. Id 0 and ids outside the set are never solid.</summary>
    public bool IsSolid(int tileId) => tileId >= 1 && tileId <= TileCount && _solid[tileId];

    /// <summary>Marks a tile solid or not. Solid tiles become collision when a <see cref="TileGridBuilder"/> builds a grid.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The id is outside 1..<see cref="TileCount"/>.</exception>
    public void SetSolid(int tileId, bool solid)
    {
        if (tileId < 1 || tileId > TileCount)
            throw new ArgumentOutOfRangeException(nameof(tileId), $"Tile id {tileId} is outside 1..{TileCount}.");
        _solid[tileId] = solid;
    }
}
