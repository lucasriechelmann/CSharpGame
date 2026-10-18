namespace Engine.Core;

/// <summary>
/// Shares the render interpolation factor with the render systems. <c>Game</c> sets <see cref="Alpha"/> once per frame
/// from the fixed timestep; systems that draw moving things read it. Systems built without a clock draw the raw simulated state.
/// </summary>
public sealed class RenderClock
{
    /// <summary>0..1 position between the last two fixed steps.</summary>
    public float Alpha { get; set; }
}
