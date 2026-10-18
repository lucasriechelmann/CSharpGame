using System.Collections.Concurrent;
using System.Diagnostics;
using Engine.Editor.Projects;

namespace Engine.Editor;

// The work behind the hub's buttons: creating and opening projects, and the build that comes first.
internal sealed partial class HubShell
{
    private readonly ConcurrentQueue<string> _incoming = new();
    private readonly List<string> _buildLog = new();
    private Task<BuildResult>? _build;
    private string _buildTitle = string.Empty;
    private string _pendingProject = string.Empty;
    private string _name = "MyGame";
    private string _location;
    private string _message = string.Empty;

    private readonly Dictionary<string, string> _names = new();

    private bool Building => _build != null;

    private bool BuildFailed => _build is { IsCompleted: true, Result.Success: false };

    private static string DefaultLocation() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Game Projects");

    private void OpenNewProjectPage()
    {
        _message = string.Empty;
        _page = Page.NewProject;
    }

    private void BrowseForProject()
    {
        string? file = NativeDialogs.PickFile("Open a game project", "Game projects", "*" + ProjectFile.Extension);
        if (file != null) OpenProject(file);
    }

    private void BrowseForLocation()
    {
        string? folder = NativeDialogs.PickFolder("Choose the folder to create the project in");
        if (folder != null) _location = folder;
    }

    private void CreateProject()
    {
        try
        {
            string file = ProjectTemplate.Create(_location.Trim(), _name, DotnetBuild.EngineDirectory);
            _message = string.Empty;
            OpenProject(file);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            _message = ex.Message;
        }
    }

    /// <summary>Builds the project's game library in the background; when that succeeds <see cref="PumpBuild"/> starts the editor on it.</summary>
    private void OpenProject(string projectFile)
    {
        ProjectFile project;
        try
        {
            project = ProjectFile.Load(projectFile);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
        {
            _message = ex.Message;
            _page = Page.Home;
            return;
        }

        ProjectTemplate.UpdateEngineDirectory(project.Root, DotnetBuild.EngineDirectory);
        _recent.Add(projectFile);
        _buildLog.Clear();
        _buildTitle = project.Name;
        _pendingProject = projectFile;
        var arguments = DotnetBuild.BuildArguments(project.GameProjectPath, "Debug", DotnetBuild.EngineDirectory);
        _build = Task.Run(() => DotnetBuild.Run(arguments, _incoming.Enqueue));
    }

    /// <summary>Called every frame: moves the build's new output lines into the log and, when the build is finished, starts the editor or shows the failure.</summary>
    private void PumpBuild()
    {
        while (_incoming.TryDequeue(out string? line)) _buildLog.Add(line);
        if (_build is not { IsCompleted: true } finished || !finished.Result.Success) return;

        _build = null;
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        start.ArgumentList.Add(_pendingProject);
        start.ArgumentList.Add("--no-build"); // just built
        Process.Start(start);
        _game.Quit();
    }

    private void ClearBuild()
    {
        _build = null;
        _page = Page.Home;
    }

    /// <summary>The name shown for a recent project: the one in its project file, or the file name when it cannot be read. Read once per path.</summary>
    private string ProjectName(string path)
    {
        if (_names.TryGetValue(path, out string? known)) return known;

        string name;
        try
        {
            name = ProjectFile.Load(path).Name;
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
        {
            name = Path.GetFileName(path);
        }

        _names[path] = name;
        return name;
    }
}
