namespace Scene3D;

/// <summary>Names of the input actions and axes this game binds.</summary>
public static class GameActions
{
    /// <summary>Fly forward / backward (W / S).</summary>
    public const string MoveForward = "MoveForward";

    /// <summary>Strafe right / left (D / A).</summary>
    public const string MoveRight = "MoveRight";

    /// <summary>Fly up / down (E / Q).</summary>
    public const string MoveUp = "MoveUp";

    /// <summary>Hold to turn the camera with the mouse (right button).</summary>
    public const string LookHeld = "LookHeld";

    /// <summary>Hold to fly faster (Shift).</summary>
    public const string Fast = "Fast";
}
