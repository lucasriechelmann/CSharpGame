namespace Engine.Core;

/// <summary>
/// Accumulator-based fixed timestep. Feed it real frame time; it tells you how many
/// fixed simulation steps to run and how far (0..1) the renderer is between steps.
/// </summary>
public sealed class FixedTimestep
{
    private double _accumulator;

    /// <summary>Creates a timestep.</summary>
    /// <param name="stepSeconds">Length of one simulation step in seconds (default 60 Hz).</param>
    /// <param name="maxFrameSeconds">Longest frame time to accept; longer frames are clamped.</param>
    public FixedTimestep(double stepSeconds = 1.0 / 60.0, double maxFrameSeconds = 0.25)
    {
        if (stepSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(stepSeconds));
        Step = stepSeconds;
        MaxFrame = maxFrameSeconds;
    }

    /// <summary>Length of one simulation step in seconds.</summary>
    public double Step { get; }

    /// <summary>Longest frame we will simulate; avoids the "spiral of death" after a stall.</summary>
    public double MaxFrame { get; }

    /// <summary>Fraction of a step left in the accumulator; use it to interpolate rendering.</summary>
    public double Alpha => _accumulator / Step;

    /// <summary>Adds a frame's elapsed time and returns the number of fixed steps to run.</summary>
    public int Advance(double frameSeconds)
    {
        _accumulator += Math.Min(Math.Max(frameSeconds, 0), MaxFrame);
        int steps = 0;
        while (_accumulator >= Step)
        {
            _accumulator -= Step;
            steps++;
        }
        return steps;
    }
}
