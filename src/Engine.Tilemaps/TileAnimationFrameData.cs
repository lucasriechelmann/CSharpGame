namespace Engine.Tilemaps;

/// <summary>JSON shape of one animation frame: which tile and how long, in seconds.</summary>
public sealed class TileAnimationFrameData
{
    /// <summary>The tile id shown during the frame.</summary>
    public int Tile { get; set; }

    /// <summary>The frame length in seconds.</summary>
    public float Duration { get; set; }
}
