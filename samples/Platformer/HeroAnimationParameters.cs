namespace Platformer;

/// <summary>
/// Names of the parameters declared in <c>Assets/anim/hero.anim.json</c>. The animation graph itself is data; these constants
/// are how code refers to its parameters without typos.
/// </summary>
public static class HeroAnimationParameters
{
    /// <summary>Horizontal speed in world units per second (absolute value).</summary>
    public const string Speed = "Speed";

    /// <summary>Vertical velocity in world units per second (positive is up).</summary>
    public const string VelocityY = "VelocityY";

    /// <summary>1 when standing on something, otherwise 0.</summary>
    public const string Grounded = "Grounded";
}
