using System.Text.Json;

namespace Engine.Editor.Projects;

/// <summary>
/// A game project as the editor sees it: a small JSON file (<c>MyGame.csgame.json</c>) at the project's root that says which C# project is the
/// game, where its compiled library ends up, and where the assets and the first scene are. Everything is a path relative to the file, so a
/// project folder can be moved, zipped or committed as it is.
/// </summary>
public sealed record ProjectFile
{
    /// <summary>The file name ending every project file has.</summary>
    public const string Extension = ".csgame.json";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>The project's display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The game library's C# project (<c>.csproj</c>), relative to the project file. The editor builds it with <c>dotnet build</c>.</summary>
    public string GameProject { get; init; } = string.Empty;

    /// <summary>The compiled game library (<c>.dll</c>) the editor loads after building, relative to the project file.</summary>
    public string GameLibrary { get; init; } = string.Empty;

    /// <summary>The launcher's C# project (the program that ships), relative to the project file; empty when the project has none.</summary>
    public string Launcher { get; init; } = string.Empty;

    /// <summary>The assets folder, relative to the project file.</summary>
    public string Assets { get; init; } = "Assets";

    /// <summary>The scene opened first, as an asset name (for example <c>scenes/main.scene.json</c>).</summary>
    public string Scene { get; init; } = string.Empty;

    /// <summary>The folder the project file is in (set by <see cref="Load"/>; empty for a file that was not loaded from disk).</summary>
    public string Root { get; init; } = string.Empty;

    /// <summary>The full path of the game's C# project.</summary>
    public string GameProjectPath => Path.GetFullPath(Path.Combine(Root, GameProject));

    /// <summary>The full path of the compiled game library.</summary>
    public string GameLibraryPath => Path.GetFullPath(Path.Combine(Root, GameLibrary));

    /// <summary>The full path of the launcher's C# project, or an empty string when there is none.</summary>
    public string LauncherPath => Launcher.Length == 0 ? string.Empty : Path.GetFullPath(Path.Combine(Root, Launcher));

    /// <summary>The full path of the assets folder.</summary>
    public string AssetsPath => Path.GetFullPath(Path.Combine(Root, Assets));

    /// <summary>Reads a project file.</summary>
    /// <param name="path">Path to a <c>.csgame.json</c> file.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not valid JSON, or names no game project or library.</exception>
    public static ProjectFile Load(string path)
    {
        string full = Path.GetFullPath(path);
        if (!File.Exists(full)) throw new FileNotFoundException($"Project file '{path}' not found.", full);

        ProjectFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ProjectFile>(File.ReadAllText(full), Json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"'{path}' is not a valid project file: {ex.Message}", ex);
        }

        if (file is null || file.GameProject.Length == 0 || file.GameLibrary.Length == 0)
            throw new InvalidDataException($"'{path}' must name a gameProject and a gameLibrary.");

        return file with { Root = Path.GetDirectoryName(full)! };
    }

    /// <summary>Writes the project file. <see cref="Root"/> is not stored; the file's own location is the root.</summary>
    /// <param name="path">Where to write.</param>
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this with { Root = string.Empty }, Json));
    }
}
