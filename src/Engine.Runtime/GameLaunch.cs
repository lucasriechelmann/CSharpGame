namespace Engine.Runtime;

/// <summary>
/// Where a game finds its files, decided by whoever starts it: the shipped launcher uses the folders next to the executable, the
/// editor opens a project folder and so edits the real source assets. Games must not look at <c>AppContext.BaseDirectory</c> themselves.
/// </summary>
/// <param name="AssetRoot">The <c>Assets</c> folder: textures, scenes, prefabs, sounds. Everything is loaded by name relative to it.</param>
/// <param name="DataRoot">A writable folder for the game's own files (quick-saves, high scores).</param>
/// <param name="EditsSource">True when <paramref name="AssetRoot"/> is the project's source folder, so saving a scene or tile layer changes files you can commit.</param>
/// <param name="Settings">The player-facing settings (window size, volume); null means the game's own defaults.</param>
public sealed record GameLaunch(string AssetRoot, string DataRoot, bool EditsSource = false, GameSettings? Settings = null)
{
    /// <summary>The shipped layout: <c>Assets</c> and the data files next to the executable.</summary>
    public static GameLaunch Default => new(Path.Combine(AppContext.BaseDirectory, "Assets"), AppContext.BaseDirectory, Settings: GameSettings.Load(Path.Combine(AppContext.BaseDirectory, GameSettings.FileName)));

    /// <summary>Opens a project folder (one that has an <c>Assets</c> folder): assets are edited in place, other files go to a <c>.local</c> folder inside it.</summary>
    /// <param name="projectFolder">The folder containing the assets folder.</param>
    /// <param name="assetsFolder">The assets folder's name inside it.</param>
    /// <exception cref="DirectoryNotFoundException">The folder has no <c>Assets</c> folder.</exception>
    public static GameLaunch ForProject(string projectFolder, string assetsFolder = "Assets")
    {
        string assets = Path.Combine(Path.GetFullPath(projectFolder), assetsFolder);
        if (!Directory.Exists(assets)) throw new DirectoryNotFoundException($"'{projectFolder}' has no {assetsFolder} folder.");

        return new GameLaunch(assets, Path.Combine(Path.GetFullPath(projectFolder), ".local"), EditsSource: true);
    }
}
