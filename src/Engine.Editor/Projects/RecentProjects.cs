using System.Text.Json;

namespace Engine.Editor.Projects;

/// <summary>The list of projects opened lately, newest first, kept in a small JSON file so the hub can offer them next time.</summary>
public sealed class RecentProjects
{
    private const int Capacity = 12;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _file;
    private readonly List<string> _paths;

    /// <summary>Opens the list stored at <paramref name="file"/> (an empty list when the file is missing or unreadable).</summary>
    /// <param name="file">Where the list is stored.</param>
    public RecentProjects(string file)
    {
        _file = file;
        _paths = Read(file);
    }

    /// <summary>The usual place: <c>%AppData%\Game Editor\recent.json</c>.</summary>
    public static string DefaultFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Game Editor", "recent.json");

    /// <summary>Full paths of the project files, newest first, leaving out any that no longer exist.</summary>
    public IReadOnlyList<string> Paths => _paths.Where(File.Exists).ToList();

    /// <summary>Puts a project at the top of the list (moving it if it was already there) and saves.</summary>
    /// <param name="projectFile">Path to a project file.</param>
    public void Add(string projectFile)
    {
        string full = Path.GetFullPath(projectFile);
        _paths.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
        _paths.Insert(0, full);
        if (_paths.Count > Capacity) _paths.RemoveRange(Capacity, _paths.Count - Capacity);
        Save();
    }

    /// <summary>Takes a project off the list and saves.</summary>
    /// <param name="projectFile">Path to a project file.</param>
    public void Remove(string projectFile)
    {
        string full = Path.GetFullPath(projectFile);
        _paths.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        File.WriteAllText(_file, JsonSerializer.Serialize(_paths, Json));
    }

    private static List<string> Read(string file)
    {
        try
        {
            return File.Exists(file) ? JsonSerializer.Deserialize<List<string>>(File.ReadAllText(file)) ?? [] : [];
        }
        catch (JsonException)
        {
            return []; // a damaged list is not worth failing over; it is rebuilt as projects are opened
        }
    }
}
