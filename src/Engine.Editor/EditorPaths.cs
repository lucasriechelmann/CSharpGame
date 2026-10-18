namespace Engine.Editor;

/// <summary>Where the editor keeps its own files, per user (not per project): the window layout and the list of recent projects.</summary>
internal static class EditorPaths
{
    /// <summary><c>%AppData%\Game Editor</c>.</summary>
    public static string AppDataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Game Editor");

    /// <summary>The saved window layout (ImGui's settings file). When it does not exist yet, the editor builds its default layout.</summary>
    public static string LayoutFile => Path.Combine(AppDataFolder, "layout.ini");
}
