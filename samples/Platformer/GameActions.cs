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

    /// <summary>Saves the whole world to a scene file (F5).</summary>
    public const string QuickSave = "QuickSave";

    /// <summary>Replaces the world with the last quick-save (F9).</summary>
    public const string QuickLoad = "QuickLoad";

    /// <summary>Switches pixel-perfect rendering on or off (F6).</summary>
    public const string TogglePixelPerfect = "TogglePixelPerfect";

    /// <summary>Switches HDR rendering with tone mapping on or off (F7).</summary>
    public const string ToggleHdr = "ToggleHdr";

    /// <summary>Cycles multisampling off, 2x, 4x, 8x (F8).</summary>
    public const string CycleMsaa = "CycleMsaa";

    /// <summary>Opens or closes the pause menu (Escape or P).</summary>
    public const string Pause = "Pause";

    /// <summary>Moves the pause menu highlight up (Up or W).</summary>
    public const string MenuUp = "MenuUp";

    /// <summary>Moves the pause menu highlight down (Down or S).</summary>
    public const string MenuDown = "MenuDown";

    /// <summary>Chooses the highlighted pause menu entry (Enter or Space).</summary>
    public const string MenuConfirm = "MenuConfirm";

    /// <summary>Starts the level over (Enter).</summary>
    public const string Restart = "Restart";
}
