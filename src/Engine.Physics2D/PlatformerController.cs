namespace Engine.Physics2D;

/// <summary>
/// Makes a <see cref="PhysicsBody"/> behave like a platformer character. Needs a <see cref="PlatformerIntent"/> (what the
/// player wants) and a <see cref="PhysicsBody"/>; <see cref="PlatformerControllerSystem"/> does the work.
/// </summary>
public struct PlatformerController
{
    /// <summary>The feel parameters. Edit these (for example from a debug panel) to tune the character live.</summary>
    public PlatformerTuning Tuning;

    /// <summary>Time left in which a jump is still allowed after leaving the ground. Runtime state.</summary>
    public float CoyoteTimer;

    /// <summary>Time left in which a remembered jump press can still fire on landing. Runtime state.</summary>
    public float JumpBufferTimer;

    /// <summary>True from the moment a jump starts until it ends (released early, apex reached, or landed). Used for variable jump height. Runtime state.</summary>
    public bool IsJumping;

    /// <summary>Creates a controller with the given tuning and cleared runtime state.</summary>
    public static PlatformerController Create(PlatformerTuning tuning) => new() { Tuning = tuning };
}
