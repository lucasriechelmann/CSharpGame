namespace Platformer;

/// <summary>Tile ids in <c>Assets/tiles.png</c> (1-based, left to right). Ids 1 to 3 are solid; the rest are decoration.</summary>
public static class TileIds
{
    /// <summary>Plain dirt (solid).</summary>
    public const int Dirt = 1;

    /// <summary>Dirt with a grass cap (solid). Used where nothing is above.</summary>
    public const int Grass = 2;

    /// <summary>Stone blocks (solid). Used for the outer walls and ceiling.</summary>
    public const int Stone = 3;

    /// <summary>Dark background brick (decoration, drawn behind everything).</summary>
    public const int Brick = 4;

    /// <summary>Grass tuft (decoration, drawn in front of characters).</summary>
    public const int Tuft = 5;
}
