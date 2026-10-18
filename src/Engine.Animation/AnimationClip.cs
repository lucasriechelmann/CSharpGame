using Engine.Rendering;

namespace Engine.Animation;

/// <summary>
/// A named sequence of sprite-sheet rectangles played at a fixed frame rate. It is pure data plus the maths that maps
/// elapsed time to a frame, so playback is deterministic and testable without a GPU.
/// </summary>
public sealed class AnimationClip
{
    private readonly Rect[] _frames;

    /// <summary>Creates a clip.</summary>
    /// <param name="name">Name for debugging and tools.</param>
    /// <param name="frames">Pixel rectangles on the sprite's texture, in play order. At least one.</param>
    /// <param name="framesPerSecond">Playback rate; must be positive.</param>
    /// <param name="loopMode">What happens after the last frame.</param>
    public AnimationClip(string name, IEnumerable<Rect> frames, float framesPerSecond, AnimationLoopMode loopMode = AnimationLoopMode.Loop)
    {
        _frames = frames.ToArray();
        if (_frames.Length == 0) throw new ArgumentException("A clip needs at least one frame.", nameof(frames));
        if (framesPerSecond <= 0f) throw new ArgumentOutOfRangeException(nameof(framesPerSecond), "Frame rate must be positive.");

        Name = name;
        FramesPerSecond = framesPerSecond;
        LoopMode = loopMode;
    }

    /// <summary>Name for debugging and tools.</summary>
    public string Name { get; }

    /// <summary>The frames, as pixel rectangles on the sprite's texture.</summary>
    public IReadOnlyList<Rect> Frames => _frames;

    /// <summary>Playback rate in frames per second.</summary>
    public float FramesPerSecond { get; }

    /// <summary>What happens after the last frame.</summary>
    public AnimationLoopMode LoopMode { get; }

    /// <summary>Seconds for one forward pass through all frames.</summary>
    public float Duration => _frames.Length / FramesPerSecond;

    /// <summary>
    /// Builds a clip from cells of a regular grid sprite sheet.
    /// </summary>
    /// <param name="name">Name for debugging and tools.</param>
    /// <param name="cellWidth">Width of one cell in pixels.</param>
    /// <param name="cellHeight">Height of one cell in pixels.</param>
    /// <param name="sheetColumns">Number of cells per row in the sheet.</param>
    /// <param name="cells">Cell indices to play, counted left to right then top to bottom from 0.</param>
    /// <param name="framesPerSecond">Playback rate.</param>
    /// <param name="loopMode">What happens after the last frame.</param>
    public static AnimationClip FromGrid(string name, int cellWidth, int cellHeight, int sheetColumns, IEnumerable<int> cells, float framesPerSecond, AnimationLoopMode loopMode = AnimationLoopMode.Loop)
    {
        if (sheetColumns <= 0) throw new ArgumentOutOfRangeException(nameof(sheetColumns));

        var frames = cells.Select(cell => new Rect(
            cell % sheetColumns * cellWidth,
            cell / sheetColumns * cellHeight,
            cellWidth,
            cellHeight));
        return new AnimationClip(name, frames, framesPerSecond, loopMode);
    }

    /// <summary>Returns the frame to show after <paramref name="time"/> seconds of playback.</summary>
    /// <param name="time">Seconds since the clip started (negative values count as zero).</param>
    /// <param name="finished">True once a <see cref="AnimationLoopMode.Once"/> clip has played all its frames; always false for looping modes.</param>
    public int GetFrameIndex(float time, out bool finished)
    {
        finished = false;
        int count = _frames.Length;
        long step = (long)MathF.Floor(MathF.Max(time, 0f) * FramesPerSecond);

        switch (LoopMode)
        {
            case AnimationLoopMode.Once:
                finished = step >= count;
                return (int)Math.Min(step, count - 1);

            case AnimationLoopMode.PingPong:
                if (count == 1) return 0;
                int cycle = 2 * count - 2;
                int position = (int)(step % cycle);
                return position < count ? position : cycle - position;

            default:
                return (int)(step % count);
        }
    }
}
