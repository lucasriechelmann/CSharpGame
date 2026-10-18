using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>
/// JSON shape of a tile set (the contents of a <c>.tileset.json</c> file): which atlas texture, how it is cut into tiles, and
/// which tile ids are solid. Columns and rows are worked out from the texture size unless given.
/// </summary>
public sealed class TileSetData
{
    /// <summary>Asset name of the atlas texture, for example <c>tiles.png</c>.</summary>
    public string Texture { get; set; } = "";

    /// <summary>Width of one tile in pixels.</summary>
    public int TileWidth { get; set; }

    /// <summary>Height of one tile in pixels.</summary>
    public int TileHeight { get; set; }

    /// <summary>Pixels between neighbouring tiles.</summary>
    public int Spacing { get; set; }

    /// <summary>Pixels around the whole grid of tiles.</summary>
    public int Margin { get; set; }

    /// <summary>Tiles per row; worked out from the texture width when omitted.</summary>
    public int? Columns { get; set; }

    /// <summary>Rows of tiles; worked out from the texture height when omitted.</summary>
    public int? Rows { get; set; }

    /// <summary>The animated tiles: each names a tile and the frames it cycles through.</summary>
    public List<TileAnimationData> Animations { get; set; } = new();

    /// <summary>Ids of the tiles that block movement.</summary>
    public List<int> Solid { get; set; } = new();

    /// <summary>Builds the tile set once the atlas texture and its size are known.</summary>
    /// <param name="texture">Handle of the loaded atlas texture.</param>
    /// <param name="atlasWidth">Texture width in pixels (used when <see cref="Columns"/> is omitted).</param>
    /// <param name="atlasHeight">Texture height in pixels (used when <see cref="Rows"/> is omitted).</param>
    /// <exception cref="InvalidOperationException">The data is inconsistent (bad sizes, or a solid id outside the set).</exception>
    public TileSet CreateTileSet(AssetHandle<Texture2D> texture, int atlasWidth, int atlasHeight)
    {
        if (TileWidth <= 0 || TileHeight <= 0)
            throw new InvalidOperationException("Tile set: 'tileWidth' and 'tileHeight' must be positive.");

        int columns = Columns ?? (atlasWidth - 2 * Margin + Spacing) / (TileWidth + Spacing);
        int rows = Rows ?? (atlasHeight - 2 * Margin + Spacing) / (TileHeight + Spacing);
        if (columns <= 0 || rows <= 0)
            throw new InvalidOperationException("Tile set: the atlas is too small for even one tile.");

        var tileSet = new TileSet(texture, TileWidth, TileHeight, columns, rows, Spacing, Margin);
        foreach (int id in Solid)
        {
            if (id < 1 || id > tileSet.TileCount)
                throw new InvalidOperationException($"Tile set: solid id {id} is outside 1..{tileSet.TileCount}.");
            tileSet.SetSolid(id, true);
        }

        foreach (var animation in Animations)
        {
            if (animation.Frames.Count == 0) throw new InvalidOperationException($"Tile set: the animation of tile {animation.Tile} has no frames.");
            tileSet.SetAnimation(animation.Tile, animation.Frames.Select(f => new TileFrame(f.Tile, f.Duration)).ToList());
        }

        return tileSet;
    }
}
