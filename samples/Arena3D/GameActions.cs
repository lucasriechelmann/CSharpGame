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

    /// <summary>Starts a new run after one has ended (Enter).</summary>
    public const string Restart = "Restart";

    /// <summary>Opens or closes the pause menu (Escape or P).</summary>
    public const string Pause = "Pause";

    /// <summary>Moves the pause menu highlight up (Up).</summary>
    public const string MenuUp = "MenuUp";

    /// <summary>Moves the pause menu highlight down (Down).</summary>
    public const string MenuDown = "MenuDown";

    /// <summary>Chooses the highlighted pause menu entry (Enter).</summary>
    public const string MenuConfirm = "MenuConfirm";
}
