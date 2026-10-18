using System.Text.RegularExpressions;

namespace Engine.Editor.Projects;

/// <summary>
/// Creates a new game project on disk: a project file, a game library (a <see cref="Runtime.Game"/> subclass and its module), a launcher (the
/// program that ships), an <c>Assets</c> folder with a first scene and a sample sprite, and a <c>Directory.Build.props</c> that points the C# projects at
/// the engine assemblies. The result builds with plain <c>dotnet build</c> and opens in the editor straight away.
/// </summary>
public static partial class ProjectTemplate
{
    private const string NameToken = "__NAME__";
    private const string EngineDirToken = "__ENGINEDIR__";

    /// <summary>Checks a project name: it becomes a C# namespace and file names, so it must start with a letter and use only letters, digits and underscores.</summary>
    /// <param name="name">The proposed name.</param>
    /// <param name="problem">Why the name is not acceptable, when it is not.</param>
    public static bool IsValidName(string name, out string problem)
    {
        if (string.IsNullOrWhiteSpace(name)) problem = "Give the project a name.";
        else if (!NamePattern().IsMatch(name)) problem = "Use letters, digits and underscores, starting with a letter.";
        else problem = string.Empty;
        return problem.Length == 0;
    }

    /// <summary>Writes a new project at <c>parentFolder\name</c> and returns the path of its project file.</summary>
    /// <param name="parentFolder">The folder to create the project folder in.</param>
    /// <param name="name">The project name (see <see cref="IsValidName"/>).</param>
    /// <param name="engineDir">The folder holding the engine assemblies, written into <c>Directory.Build.props</c> so building outside the editor works too.</param>
    /// <exception cref="ArgumentException">The name is not valid.</exception>
    /// <exception cref="IOException">The project folder already exists and is not empty.</exception>
    public static string Create(string parentFolder, string name, string engineDir)
    {
        if (!IsValidName(name, out string problem)) throw new ArgumentException(problem, nameof(name));

        string root = Path.GetFullPath(Path.Combine(parentFolder, name));
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
            throw new IOException($"The folder '{root}' already exists and is not empty.");

        string engine = engineDir.EndsWith(Path.DirectorySeparatorChar) ? engineDir : engineDir + Path.DirectorySeparatorChar;
        foreach (var (path, content) in Files)
            Write(Path.Combine(root, Fill(path, name, engine)), Fill(content, name, engine));

        Directory.CreateDirectory(Path.Combine(root, "Assets", "sprites"));
        File.WriteAllBytes(Path.Combine(root, "Assets", "sprites", "square.png"), SquareSprite());

        string projectFile = Path.Combine(root, name + ProjectFile.Extension);
        new ProjectFile
        {
            Name = name,
            GameProject = $"Game/{name}.Game.csproj",
            GameLibrary = $"Game/bin/Debug/net10.0/{name}.Game.dll",
            Launcher = $"Launcher/{name}.csproj",
            Assets = "Assets",
            Scene = "scenes/main.scene.json",
        }.Save(projectFile);
        return projectFile;
    }

    /// <summary>Points an existing project's <c>Directory.Build.props</c> at the engine assemblies of this editor, for a project made by an older or moved editor.</summary>
    /// <param name="projectRoot">The project folder.</param>
    /// <param name="engineDir">The folder holding the engine assemblies.</param>
    public static void UpdateEngineDirectory(string projectRoot, string engineDir)
    {
        string props = Path.Combine(projectRoot, "Directory.Build.props");
        if (!File.Exists(props)) return;

        string engine = engineDir.EndsWith(Path.DirectorySeparatorChar) ? engineDir : engineDir + Path.DirectorySeparatorChar;
        string text = File.ReadAllText(props);
        string updated = EngineDirPattern().Replace(text, $"<EngineDir Condition=\"'$(EngineDir)' == ''\">{engine}</EngineDir>");
        if (updated != text) File.WriteAllText(props, updated);
    }

    private static string Fill(string text, string name, string engineDir) => text.Replace(NameToken, name).Replace(EngineDirToken, engineDir);

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content.Replace("\r\n", "\n"));
    }

    /// <summary>A 32x32 white square with a soft grey border: the sample sprite, tinted by colour in the scene.</summary>
    private static byte[] SquareSprite()
    {
        const int size = 32;
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                byte level = border ? (byte)190 : (byte)255;
                int i = (y * size + x) * 4;
                pixels[i] = level;
                pixels[i + 1] = level;
                pixels[i + 2] = level;
                pixels[i + 3] = 255;
            }
        }

        return PngWriter.Encode(size, size, pixels);
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$")]
    private static partial Regex NamePattern();

    [GeneratedRegex("<EngineDir Condition=\"'\\$\\(EngineDir\\)' == ''\">[^<]*</EngineDir>")]
    private static partial Regex EngineDirPattern();
}
