using Engine.Core.Ecs;
using Engine.Runtime;

namespace Engine.Editor.Tests;

/// <summary>A module for the loader to find: this test assembly contains exactly one.</summary>
public sealed class FakeGameModule : IGameModule
{
    /// <inheritdoc />
    public string Name => "Fake";

    /// <inheritdoc />
    public Game CreateGame(GameLaunch launch) => throw new NotSupportedException("Creating a game needs a window.");
}

public class GameLibraryTests
{
    [Fact]
    public void The_one_module_in_an_assembly_is_found_and_created()
    {
        var module = GameLibrary.CreateModule(typeof(FakeGameModule).Assembly);

        Assert.IsType<FakeGameModule>(module);
        Assert.Equal("Fake", module.Name);
    }

    [Fact]
    public void An_assembly_without_a_module_is_rejected_with_its_name()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => GameLibrary.CreateModule(typeof(World).Assembly));

        Assert.Contains("Engine.Core", ex.Message);
    }

    [Fact]
    public void A_missing_library_file_is_reported_as_not_found()
    {
        Assert.Throws<FileNotFoundException>(() => GameLibrary.Load(Path.Combine(Path.GetTempPath(), "nope-" + Guid.NewGuid().ToString("N") + ".dll")));
    }
}
