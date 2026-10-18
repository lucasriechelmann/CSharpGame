using Engine.Editor.Projects;

namespace Engine.Editor.Tests;

public sealed class ProjectTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "projects-" + Guid.NewGuid().ToString("N"));

    public ProjectTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void A_project_file_round_trips_and_resolves_paths_against_its_folder()
    {
        string path = Path.Combine(_folder, "Demo" + ProjectFile.Extension);
        new ProjectFile { Name = "Demo", GameProject = "Game/Demo.Game.csproj", GameLibrary = "Game/bin/Debug/net10.0/Demo.Game.dll", Launcher = "Launcher/Demo.csproj", Scene = "scenes/main.scene.json" }.Save(path);

        var loaded = ProjectFile.Load(path);

        Assert.Equal("Demo", loaded.Name);
        Assert.Equal(_folder, loaded.Root);
        Assert.Equal(Path.Combine(_folder, "Game", "Demo.Game.csproj"), loaded.GameProjectPath);
        Assert.Equal(Path.Combine(_folder, "Game", "bin", "Debug", "net10.0", "Demo.Game.dll"), loaded.GameLibraryPath);
        Assert.Equal(Path.Combine(_folder, "Assets"), loaded.AssetsPath);
        Assert.Equal(Path.Combine(_folder, "Launcher", "Demo.csproj"), loaded.LauncherPath);
    }

    [Fact]
    public void A_project_without_a_launcher_has_an_empty_launcher_path()
    {
        string path = Path.Combine(_folder, "A" + ProjectFile.Extension);
        new ProjectFile { Name = "A", GameProject = "a.csproj", GameLibrary = "a.dll" }.Save(path);

        Assert.Equal(string.Empty, ProjectFile.Load(path).LauncherPath);
    }

    [Fact]
    public void Missing_broken_and_incomplete_project_files_are_reported()
    {
        Assert.Throws<FileNotFoundException>(() => ProjectFile.Load(Path.Combine(_folder, "nope.csgame.json")));

        string broken = Path.Combine(_folder, "broken.csgame.json");
        File.WriteAllText(broken, "{ not json");
        Assert.Throws<InvalidDataException>(() => ProjectFile.Load(broken));

        string incomplete = Path.Combine(_folder, "incomplete.csgame.json");
        File.WriteAllText(incomplete, """{ "name": "X" }""");
        Assert.Throws<InvalidDataException>(() => ProjectFile.Load(incomplete));
    }

    [Fact]
    public void Recent_projects_keep_newest_first_without_duplicates_and_skip_deleted_files()
    {
        string a = Path.Combine(_folder, "a.csgame.json");
        string b = Path.Combine(_folder, "b.csgame.json");
        File.WriteAllText(a, "{}");
        File.WriteAllText(b, "{}");
        var recent = new RecentProjects(Path.Combine(_folder, "recent.json"));

        recent.Add(a);
        recent.Add(b);
        recent.Add(a);

        Assert.Equal(new[] { a, b }, recent.Paths);
        Assert.Equal(new[] { a, b }, new RecentProjects(Path.Combine(_folder, "recent.json")).Paths); // saved

        File.Delete(b);
        Assert.Equal(new[] { a }, recent.Paths);

        recent.Remove(a);
        Assert.Empty(recent.Paths);
    }

    [Fact]
    public void A_damaged_recent_list_starts_empty()
    {
        string file = Path.Combine(_folder, "recent.json");
        File.WriteAllText(file, "][");

        Assert.Empty(new RecentProjects(file).Paths);
    }

    [Theory]
    [InlineData("MyGame", true)]
    [InlineData("game_2", true)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("2Cool", false)]
    [InlineData("My Game", false)]
    [InlineData("a-b", false)]
    public void Project_names_must_be_usable_as_a_csharp_namespace(string name, bool valid)
    {
        Assert.Equal(valid, ProjectTemplate.IsValidName(name, out string problem));
        Assert.Equal(valid, problem.Length == 0);
    }

    [Fact]
    public void Creating_a_project_writes_the_files_with_the_name_filled_in()
    {
        string file = ProjectTemplate.Create(_folder, "Demo", @"C:\Engine\");

        var project = ProjectFile.Load(file);
        Assert.Equal(Path.Combine(_folder, "Demo", "Demo.csgame.json"), file);
        Assert.True(File.Exists(project.GameProjectPath));
        Assert.True(File.Exists(project.LauncherPath));
        Assert.True(File.Exists(Path.Combine(project.AssetsPath, "scenes", "main.scene.json")));
        Assert.True(File.Exists(Path.Combine(project.AssetsPath, "sprites", "square.png")));

        string game = File.ReadAllText(Path.Combine(project.Root, "Game", "DemoGame.cs"));
        Assert.Contains("class DemoGame : Game", game);
        Assert.DoesNotContain("__NAME__", game);
        Assert.DoesNotContain("__NAME__", File.ReadAllText(Path.Combine(project.Root, "Launcher", "Program.cs")));
        Assert.Contains(@"C:\Engine\", File.ReadAllText(Path.Combine(project.Root, "Directory.Build.props")));
    }

    [Fact]
    public void The_sample_sprite_is_a_png()
    {
        ProjectTemplate.Create(_folder, "Demo", @"C:\Engine\");

        byte[] bytes = File.ReadAllBytes(Path.Combine(_folder, "Demo", "Assets", "sprites", "square.png"));

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, bytes[..8]);
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));
    }

    [Fact]
    public void A_project_cannot_be_created_over_a_non_empty_folder_or_with_a_bad_name()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "Taken"));
        File.WriteAllText(Path.Combine(_folder, "Taken", "x.txt"), "x");

        Assert.Throws<IOException>(() => ProjectTemplate.Create(_folder, "Taken", @"C:\Engine\"));
        Assert.Throws<ArgumentException>(() => ProjectTemplate.Create(_folder, "not valid", @"C:\Engine\"));
    }

    [Fact]
    public void The_engine_directory_in_an_existing_project_can_be_updated()
    {
        string file = ProjectTemplate.Create(_folder, "Demo", @"C:\Old\");
        string root = ProjectFile.Load(file).Root;

        ProjectTemplate.UpdateEngineDirectory(root, @"D:\New");

        string props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        Assert.Contains(@"D:\New\", props);
        Assert.DoesNotContain(@"C:\Old\", props);
    }

    [Fact]
    public void Build_and_publish_arguments_pass_the_engine_directory_and_never_go_through_a_shell()
    {
        var build = DotnetBuild.BuildArguments(@"C:\p\Game.csproj", "Debug", @"C:\Engine\");
        var publish = DotnetBuild.PublishArguments(@"C:\p\Launcher.csproj", "win-x64", @"C:\out", @"C:\Engine\");

        Assert.Equal("build", build[0]);
        Assert.Contains(@"-p:EngineDir=C:\Engine\", build);
        Assert.Equal("publish", publish[0]);
        Assert.Contains("--self-contained", publish);
        Assert.Contains("win-x64", publish);
        Assert.Contains(@"C:\out", publish);
    }

    [Fact]
    public void The_png_writer_checks_its_input()
    {
        Assert.Throws<ArgumentException>(() => PngWriter.Encode(2, 2, new byte[3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngWriter.Encode(0, 2, new byte[0]));
    }
}
