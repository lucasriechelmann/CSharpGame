namespace Engine.Physics2D;

/// <summary>
/// What the player (or an AI) wants this character to do. An input system writes it once per frame; the fixed-step
/// <see cref="PlatformerControllerSystem"/> reads it. This split is what keeps input edges from being lost or repeated
/// when several (or zero) fixed steps run in one frame.
/// </summary>
public struct PlatformerIntent
{
    /// <summary>Horizontal direction in -1..1 (negative = left).</summary>
    public float MoveX;

    /// <summary>True while the jump button is held. Releasing it early shortens the jump.</summary>
    public bool JumpHeld;

    /// <summary>
    /// Set to true by the input system on the frame jump is pressed. The controller consumes it exactly once (turning it
    /// into a jump buffer) and clears it, so it survives frames where no fixed step runs.
    /// </summary>
    public bool JumpPressedPending;
}
