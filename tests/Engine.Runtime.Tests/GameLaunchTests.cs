using Engine.Runtime;

namespace Engine.Runtime.Tests;

public sealed class GameLaunchTests : IDisposable
{
    private readonly string _project = Path.Combine(Path.GetTempPath(), "launch-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_project)) Directory.Delete(_project, recursive: true);
    }

    [Fact]
    public void The_default_launch_uses_folders_next_to_the_executable()
    {
        var launch = GameLaunch.Default;

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "Assets"), launch.AssetRoot);
        Assert.Equal(AppContext.BaseDirectory, launch.DataRoot);
        Assert.False(launch.EditsSource);
    }

    [Fact]
    public void A_project_launch_edits_the_source_assets_and_keeps_its_own_data_apart()
    {
        Directory.CreateDirectory(Path.Combine(_project, "Assets"));

        var launch = GameLaunch.ForProject(_project);

        Assert.Equal(Path.Combine(_project, "Assets"), launch.AssetRoot);
        Assert.Equal(Path.Combine(_project, ".local"), launch.DataRoot);
        Assert.True(launch.EditsSource);
    }

    [Fact]
    public void A_folder_without_assets_is_not_a_project()
    {
        Directory.CreateDirectory(_project);

        Assert.Throws<DirectoryNotFoundException>(() => GameLaunch.ForProject(_project));
    }
}
