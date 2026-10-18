namespace Engine.Physics3D;

/// <summary>World-wide physics tuning shared by every body.</summary>
public sealed class PhysicsSettings3D
{
    /// <summary>Gravity acceleration in world units (metres) per second squared. Negative pulls down (Y is up).</summary>
    public float Gravity { get; set; } = -9.81f;

    /// <summary>Fastest allowed downward speed in world units per second (terminal velocity); keeps a long fall under control.</summary>
    public float MaxFallSpeed { get; set; } = 50f;

    /// <summary>
    /// How fast a body that is standing on something loses horizontal speed, in world units per second squared (a constant
    /// deceleration, like dry sliding friction: it stops a thrown crate after <c>v^2 / (2 * friction)</c> metres). 0 makes the floor
    /// frictionless, so a body keeps sliding until it hits something.
    /// </summary>
    public float GroundFriction { get; set; } = 12f;
}
