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

    /// <summary>Throws a crate from the camera (Space).</summary>
    public const string Drop = "Drop";

    /// <summary>Casts a ray from the cursor into the world and reports what it hits (left mouse button).</summary>
    public const string Pick = "Pick";

    /// <summary>Hold to fly faster (Shift).</summary>
    public const string Fast = "Fast";
}
