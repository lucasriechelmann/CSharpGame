namespace Platformer;

/// <summary>Names of the input actions and axes this game binds. Using constants avoids typos in string keys.</summary>
public static class GameActions
{
    /// <summary>Horizontal movement axis (A/D or Left/Right).</summary>
    public const string MoveX = "MoveX";

    /// <summary>Jump (Space, W or Up).</summary>
    public const string Jump = "Jump";

    /// <summary>Camera zoom axis (Q zooms out, E zooms in).</summary>
    public const string Zoom = "Zoom";

    /// <summary>Spawns creatures at the mouse cursor (left mouse button).</summary>
    public const string Spawn = "Spawn";

    /// <summary>Snaps the camera back to the hero at zoom 1 (R).</summary>
    public const string ResetCamera = "ResetCamera";
}
