namespace Engine.Physics2D;

/// <summary>The numbers that define how a platformer character feels. All speeds are world units per second, all times are seconds.</summary>
public struct PlatformerTuning
{
    /// <summary>Top horizontal speed.</summary>
    public float MoveSpeed;

    /// <summary>How quickly horizontal speed builds up on the ground (units/s per second).</summary>
    public float GroundAcceleration;

    /// <summary>How quickly horizontal speed drops to zero on the ground when there is no input.</summary>
    public float GroundDeceleration;

    /// <summary>How quickly horizontal speed builds up in the air. Usually lower than on the ground.</summary>
    public float AirAcceleration;

    /// <summary>How quickly horizontal speed drops to zero in the air when there is no input.</summary>
    public float AirDeceleration;

    /// <summary>Upward speed given at the start of a jump. See <see cref="PhysicsSettings.JumpSpeedForHeight"/>.</summary>
    public float JumpSpeed;

    /// <summary>Grace period after walking off a ledge during which a jump still works ("coyote time").</summary>
    public float CoyoteTime;

    /// <summary>How long a jump press is remembered before landing; if the character lands within it, the jump fires.</summary>
    public float JumpBufferTime;

    /// <summary>Fraction of upward speed kept when jump is released early; below 1 gives variable jump height.</summary>
    public float JumpCutMultiplier;

    /// <summary>Gravity multiplier while falling. Above 1 makes jumps feel snappier (less floaty).</summary>
    public float FallGravityMultiplier;

    /// <summary>A reasonable starting point for a pixel-scale platformer (about 3 tiles of jump height at 32-unit tiles).</summary>
    public static PlatformerTuning Default => new()
    {
        MoveSpeed = 220f,
        GroundAcceleration = 2200f,
        GroundDeceleration = 2600f,
        AirAcceleration = 1500f,
        AirDeceleration = 900f,
        JumpSpeed = 640f,
        CoyoteTime = 0.1f,
        JumpBufferTime = 0.1f,
        JumpCutMultiplier = 0.5f,
        FallGravityMultiplier = 1.6f,
    };
}
