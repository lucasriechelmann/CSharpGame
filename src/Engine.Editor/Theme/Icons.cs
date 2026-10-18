namespace Engine.Editor.Theme;

/// <summary>
/// Code points of Segoe Fluent Icons (the icon font of Windows 11, merged into the editor's font by <see cref="Win11Theme"/>) as strings, so they can be
/// put in front of a label: <c>ImGui.Button(Icons.Play + " Play")</c>. When the icon font is not installed they draw as empty boxes.
/// </summary>
public static class Icons
{
    /// <summary>Play triangle.</summary>
    public const string Play = "";

    /// <summary>Pause bars.</summary>
    public const string Pause = "";

    /// <summary>Stop square.</summary>
    public const string Stop = "";

    /// <summary>Plus sign.</summary>
    public const string Add = "";

    /// <summary>Folder.</summary>
    public const string Folder = "";

    /// <summary>Open folder.</summary>
    public const string FolderOpen = "";

    /// <summary>Document.</summary>
    public const string Document = "";

    /// <summary>Save (floppy disk).</summary>
    public const string Save = "";

    /// <summary>Refresh arrows.</summary>
    public const string Refresh = "";

    /// <summary>Hammer (build).</summary>
    public const string Build = "";

    /// <summary>Delete (bin).</summary>
    public const string Delete = "";

    /// <summary>Copy.</summary>
    public const string Copy = "";

    /// <summary>Undo arrow.</summary>
    public const string Undo = "";

    /// <summary>Redo arrow.</summary>
    public const string Redo = "";

    /// <summary>Settings gear.</summary>
    public const string Settings = "";

    /// <summary>Game controller.</summary>
    public const string Game = "";

    /// <summary>Picture.</summary>
    public const string Image = "";

    /// <summary>Music note.</summary>
    public const string Audio = "";

    /// <summary>Chevron pointing right.</summary>
    public const string ChevronRight = "";
}
