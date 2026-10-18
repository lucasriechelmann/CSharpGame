using Engine.Runtime;

namespace Engine.Runtime.Tests;

public sealed class GameSettingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "settings-" + Guid.NewGuid().ToString("N"));

    public GameSettingsTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void A_missing_file_means_the_defaults()
    {
        var settings = GameSettings.Load(Path.Combine(_folder, "none.json"));

        Assert.Null(settings.WindowWidth);
        Assert.Null(settings.WindowHeight);
        Assert.False(settings.Fullscreen);
        Assert.True(settings.VSync);
        Assert.Equal(1f, settings.MasterVolume);
    }

    [Fact]
    public void Settings_round_trip_and_unlisted_fields_keep_their_defaults()
    {
        string path = Path.Combine(_folder, GameSettings.FileName);
        new GameSettings { WindowWidth = 800, WindowHeight = 600, Fullscreen = true, VSync = false, MasterVolume = 0.25f }.Save(path);
        var loaded = GameSettings.Load(path);

        Assert.Equal(800, loaded.WindowWidth);
        Assert.Equal(600, loaded.WindowHeight);
        Assert.True(loaded.Fullscreen);
        Assert.False(loaded.VSync);
        Assert.Equal(0.25f, loaded.MasterVolume);

        File.WriteAllText(path, """{ "windowWidth": 640 }""");
        var partial = GameSettings.Load(path);
        Assert.Equal(640, partial.WindowWidth);
        Assert.Null(partial.WindowHeight);
        Assert.True(partial.VSync);
    }

    [Fact]
    public void Broken_or_impossible_settings_fail_loudly()
    {
        string path = Path.Combine(_folder, GameSettings.FileName);

        File.WriteAllText(path, "{ nope");
        Assert.Throws<InvalidDataException>(() => GameSettings.Load(path));

        File.WriteAllText(path, """{ "windowWidth": 0 }""");
        Assert.Throws<InvalidDataException>(() => GameSettings.Load(path));

        File.WriteAllText(path, """{ "masterVolume": 3 }""");
        Assert.Throws<InvalidDataException>(() => GameSettings.Load(path));
    }
}
