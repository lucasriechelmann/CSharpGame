namespace Arena3D;

/// <summary>Names of the input actions and axes this game binds.</summary>
public static class GameActions
{
    /// <summary>Walk forward / backward relative to the camera (W / S).</summary>
    public const string MoveForward = "MoveForward";

    /// <summary>Strafe right / left relative to the camera (D / A).</summary>
    public const string MoveRight = "MoveRight";

    /// <summary>Jump (Space).</summary>
    public const string Jump = "Jump";

    /// <summary>Hold to turn the camera around the player with the mouse (right button).</summary>
    public const string LookHeld = "LookHeld";

    /// <summary>Starts a new run after one has ended (Enter).</summary>
    public const string Restart = "Restart";
}
