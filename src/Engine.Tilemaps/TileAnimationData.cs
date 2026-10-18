namespace Engine.Tilemaps;

/// <summary>JSON shape of one animated tile in a <c>.tileset.json</c> file: <c>{ "tile": 5, "frames": [ { "tile": 5, "duration": 0.2 }, { "tile": 6, "duration": 0.2 } ] }</c>.</summary>
public sealed class TileAnimationData
{
    /// <summary>The tile id that animates.</summary>
    public int Tile { get; set; }

    /// <summary>The frames, in order; the animation loops.</summary>
    public List<TileAnimationFrameData> Frames { get; set; } = new();
}
