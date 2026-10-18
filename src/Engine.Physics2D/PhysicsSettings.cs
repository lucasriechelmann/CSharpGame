namespace Engine.Physics2D;

/// <summary>World-wide physics tuning shared by every body.</summary>
public sealed class PhysicsSettings
{
    /// <summary>Gravity acceleration in world units per second squared. Negative pulls down (Y is up).</summary>
    public float Gravity { get; set; } = -1800f;

    /// <summary>Fastest allowed downward speed in world units per second. Keeps a long fall from tunnelling or feeling out of control.</summary>
    public float MaxFallSpeed { get; set; } = 1200f;

    /// <summary>The upward launch speed that reaches <paramref name="height"/> world units under <see cref="Gravity"/> (ignoring fall multipliers).</summary>
    /// <param name="height">Desired jump apex height above the take-off point.</param>
    public float JumpSpeedForHeight(float height) => MathF.Sqrt(2f * MathF.Abs(Gravity) * height);
}
