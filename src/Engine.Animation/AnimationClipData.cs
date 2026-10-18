using Engine.Rendering;

namespace Engine.Animation;

/// <summary>JSON shape of a clip. Give the frames either as explicit pixel rectangles (<see cref="Frames"/>) or as sheet cells (<see cref="Grid"/>), not both.</summary>
public sealed class AnimationClipData
{
    /// <summary>Playback rate in frames per second.</summary>
    public float Fps { get; set; }

    /// <summary>What happens after the last frame.</summary>
    public AnimationLoopMode Loop { get; set; } = AnimationLoopMode.Loop;

    /// <summary>Explicit frame rectangles in pixels.</summary>
    public List<Rect>? Frames { get; set; }

    /// <summary>Frames given as cells of a regular grid sheet.</summary>
    public AnimationGridData? Grid { get; set; }
}
