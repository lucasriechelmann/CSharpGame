namespace Engine.Tilemaps;

/// <summary>JSON shape of a tile layer (the contents of a <c>.layer.json</c> file). Rows are listed top to bottom, like the picture of the level.</summary>
public sealed class TileLayerData
{
    /// <summary>Side length of one cell in world units.</summary>
    public float TileSize { get; set; }

    /// <summary>Layer cell column of the first (leftmost) tile in every row.</summary>
    public int OriginX { get; set; }

    /// <summary>Layer cell row of the <b>bottom</b> row. The first listed row is the top one, at <c>OriginY + rows - 1</c>.</summary>
    public int OriginY { get; set; }

    /// <summary>Tile ids, one array per row, top row first. 0 means empty.</summary>
    public List<int[]> Rows { get; set; } = new();
}
