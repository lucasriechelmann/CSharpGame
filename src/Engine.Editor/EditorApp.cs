using Engine.Editor.Projects;
using Engine.Editor.Theme;
using Engine.Runtime;

namespace Engine.Editor;

/// <summary>
/// What the editor program does with its command line: no arguments shows the project hub (new project, open project, recent projects); a project
/// file opens that project in the editor; <c>--new</c> and <c>--publish</c> do the same jobs without a window, which is how they are scripted and tested.
/// </summary>
public static class EditorApp
{
    /// <summary>Runs the program.</summary>
    /// <param name="args">The command line.</param>
    /// <returns>The exit code: 0 on success, 1 when something failed (the reason is printed), 2 for a wrong command line.</returns>
    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0) return RunHub();
            if (args[0] == "--new") return args.Length == 3 ? CreateProject(args[1], args[2]) : Usage();
            if (args[0] == "--publish") return args.Length >= 2 ? PublishProject(args[1], args.Length >= 3 ? args[2] : null) : Usage();
            if (args[0].StartsWith("--", StringComparison.Ordinal)) return Usage();

            return OpenProject(args[0], noBuild: args.Contains("--no-build"));
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or InvalidDataException or InvalidOperationException or IOException or ArgumentException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int RunHub()
    {
        using var game = new HubGame();
        game.ImGuiSetup = () =>
        {
            Win11Theme.Configure();
            Win11Theme.UseLayoutFile(null); // the hub has no layout to remember
        };
        game.DarkTitleBar = true;
        game.AttachEditor(context => new HubShell(context.Game, new RecentProjects(RecentProjects.DefaultFile)));
        game.Run();
        return 0;
    }

    private static int CreateProject(string parentFolder, string name)
    {
        string file = ProjectTemplate.Create(parentFolder, name, DotnetBuild.EngineDirectory);
        Console.WriteLine($"Created {file}");
        var project = ProjectFile.Load(file);
        if (!Build(project.GameProjectPath, "Debug")) return 1;
        if (project.Launcher.Length > 0 && !Build(project.LauncherPath, "Debug")) return 1;
        Console.WriteLine(file);
        return 0;
    }

    private static int PublishProject(string projectFile, string? output)
    {
        var project = ProjectFile.Load(projectFile);
        if (project.Launcher.Length == 0) throw new InvalidOperationException("This project has no launcher to publish.");

        string folder = output ?? Path.Combine(project.Root, "Build", "win-x64");
        var result = DotnetBuild.Run(DotnetBuild.PublishArguments(project.LauncherPath, "win-x64", folder, DotnetBuild.EngineDirectory), Console.WriteLine);
        if (result.Success) Console.WriteLine($"Published to {folder}");
        return result.Success ? 0 : 1;
    }

    private static int OpenProject(string projectFile, bool noBuild)
    {
        var project = ProjectFile.Load(projectFile);
        ProjectTemplate.UpdateEngineDirectory(project.Root, DotnetBuild.EngineDirectory);
        if (!noBuild && !Build(project.GameProjectPath, "Debug")) return 1;

        new RecentProjects(RecentProjects.DefaultFile).Add(projectFile);
        var module = GameLibrary.Load(project.GameLibraryPath);
        using var game = module.CreateGame(GameLaunch.ForProject(project.Root, project.Assets));
        game.ImGuiSetup = () =>
        {
            Win11Theme.Configure(); // Windows 11 look: Segoe UI, Fluent colours, rounded controls
            Win11Theme.UseLayoutFile(EditorPaths.LayoutFile);
        };
        game.DarkTitleBar = true;
        game.WindowTitle = $"{project.Name} - Game Editor";
        game.AttachEditor(context =>
        {
            var editor = new SceneEditor(context.World, context.Assets, context.Inspector, context.Log, context.Hooks, context.Game) { Pinned = true };
            editor.Toggle(); // the standalone editor is always on
            return editor;
        });
        game.Run();
        return 0;
    }

    private static bool Build(string csproj, string configuration)
    {
        Console.WriteLine($"Building {Path.GetFileName(csproj)}...");
        var result = DotnetBuild.Run(DotnetBuild.BuildArguments(csproj, configuration, DotnetBuild.EngineDirectory), Console.WriteLine);
        if (!result.Success) Console.Error.WriteLine("The build failed.");
        return result.Success;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  Editor                                   show the project hub");
        Console.Error.WriteLine("  Editor <project.csgame.json> [--no-build] open a project");
        Console.Error.WriteLine("  Editor --new <parent folder> <name>       create and build a new project");
        Console.Error.WriteLine("  Editor --publish <project.csgame.json> [output folder]");
        return 2;
    }
}
