using Engine.Core.Assets;
using Engine.Core.Logging;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Assets;

public sealed class AssetReloadTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "csharpgame-reload-" + Guid.NewGuid().ToString("N"));
    private readonly AssetManager _assets;
    private readonly MemoryLogSink _logSink = new();
    private readonly Logger _log;
    private DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public AssetReloadTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "sub"));
        Write("hello.txt", "v1");
        Write("sub/nested.txt", "n1");

        _assets = new AssetManager(_root);
        _assets.Register(new TextLoader());
        _log = new Logger().AddSink(_logSink);
        _log.MinimumLevel = LogLevel.Debug;
    }

    public void Dispose()
    {
        _assets.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    private string PathOf(string name) => Path.Combine(_root, name);

    private void Write(string name, string text) => File.WriteAllText(PathOf(name), text);

    private AssetHotReloader CreateReloader() => new(_assets, _root, _log, () => _now);

    private void Wait(double seconds) => _now = _now.AddSeconds(seconds);

    private IEnumerable<string> Messages()
    {
        for (int i = 0; i < _logSink.Count; i++) yield return _logSink[i].Message;
    }

    // ---- AssetManager.Reload ----

    [Fact]
    public void Reload_replaces_the_asset_behind_the_same_handle()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        Write("hello.txt", "v2");

        int count = _assets.Reload("hello.txt");

        Assert.Equal(1, count);
        Assert.Equal("v2", _assets.Get(handle).Text);
    }

    [Fact]
    public void Reload_of_something_never_loaded_does_nothing()
    {
        Assert.Equal(0, _assets.Reload("hello.txt"));
        Assert.Equal(0, _assets.Count);
    }

    [Fact]
    public void Reload_accepts_backslashes_and_a_leading_dot_slash()
    {
        var handle = _assets.Load<TextAsset>("sub/nested.txt");
        Write("sub/nested.txt", "n2");

        Assert.Equal(1, _assets.Reload(@".\sub\nested.txt"));
        Assert.Equal("n2", _assets.Get(handle).Text);
    }

    [Fact]
    public void Reload_can_read_from_another_file()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        Write("sub/nested.txt", "from elsewhere");

        _assets.Reload("hello.txt", PathOf("sub/nested.txt"));

        Assert.Equal("from elsewhere", _assets.Get(handle).Text);
    }

    [Fact]
    public void Reload_does_not_add_assets_or_change_the_handle()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");

        _assets.Reload("hello.txt");

        Assert.Equal(1, _assets.Count);
        Assert.Equal(handle, _assets.Load<TextAsset>("hello.txt"));
    }

    [Fact]
    public void Reload_when_the_file_was_deleted_fails_and_keeps_the_old_asset()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        File.Delete(PathOf("hello.txt"));

        Assert.Throws<FileNotFoundException>(() => _assets.Reload("hello.txt"));
        Assert.Equal("v1", _assets.Get(handle).Text);
    }

    [Fact]
    public void Reload_disposes_the_old_asset()
    {
        var manager = new AssetManager(_root);
        manager.Register(new CountingLoader());
        var handle = manager.Load<DisposableAsset>("hello.txt");
        var old = manager.Get(handle);

        manager.Reload("hello.txt");

        Assert.Equal(1, old.DisposeCount);
        Assert.NotSame(old, manager.Get(handle));
    }

    [Fact]
    public void A_loader_that_throws_leaves_the_old_asset_in_place()
    {
        var manager = new AssetManager(_root);
        var loader = new CountingLoader();
        manager.Register(loader);
        var handle = manager.Load<DisposableAsset>("hello.txt");
        var old = manager.Get(handle);
        loader.Fail = true;

        Assert.Throws<InvalidDataException>(() => manager.Reload("hello.txt"));

        Assert.Same(old, manager.Get(handle));
        Assert.Equal(0, old.DisposeCount);
    }

    private sealed class CountingLoader : IAssetLoader<DisposableAsset>
    {
        public bool Fail { get; set; }

        public DisposableAsset Load(string fullPath) => Fail ? throw new InvalidDataException("corrupt") : new DisposableAsset();
    }

    // ---- AssetHotReloader ----

    [Fact]
    public void A_change_is_reloaded_after_the_quiet_period()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        var reloader = CreateReloader();
        Write("hello.txt", "v2");

        reloader.Notify(PathOf("hello.txt"));
        Assert.Equal(0, reloader.Poll()); // too soon: the writer may not be finished
        Assert.Equal("v1", _assets.Get(handle).Text);

        Wait(AssetHotReloader.QuietSeconds);
        Assert.Equal(1, reloader.Poll());
        Assert.Equal("v2", _assets.Get(handle).Text);
        Assert.Equal(0, reloader.PendingCount);
    }

    [Fact]
    public void Repeated_events_for_one_file_reload_it_once_after_the_last_one()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        var reloader = CreateReloader();

        for (int i = 0; i < 4; i++)
        {
            Write("hello.txt", "v" + (i + 2));
            reloader.Notify(PathOf("hello.txt"));
            Wait(0.1); // keeps the file "busy" for longer than any single gap
        }

        Assert.Equal(0, reloader.Poll());
        Wait(AssetHotReloader.QuietSeconds);
        Assert.Equal(1, reloader.Poll());
        Assert.Equal("v5", _assets.Get(handle).Text);
    }

    [Fact]
    public void A_changed_file_that_is_not_a_loaded_asset_is_ignored()
    {
        var reloader = CreateReloader();
        Write("notes.txt", "scratch");

        reloader.Notify(PathOf("notes.txt"));
        Wait(1);

        Assert.Equal(0, reloader.Poll());
        Assert.Equal(0, _assets.Count);
    }

    [Fact]
    public void A_file_outside_the_watched_folder_is_ignored()
    {
        var reloader = CreateReloader();

        reloader.Notify(Path.Combine(Path.GetTempPath(), "somewhere-else.txt"));

        Assert.Equal(0, reloader.PendingCount);
    }

    [Fact]
    public void Nested_files_are_named_relative_to_the_watched_folder()
    {
        var handle = _assets.Load<TextAsset>("sub/nested.txt");
        var reloader = CreateReloader();
        Write("sub/nested.txt", "n2");

        reloader.Notify(PathOf("sub/nested.txt"));
        Wait(1);
        reloader.Poll();

        Assert.Equal("n2", _assets.Get(handle).Text);
    }

    [Fact]
    public void A_file_the_game_wrote_itself_is_not_reloaded()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        var reloader = CreateReloader();
        Write("hello.txt", "saved by the editor");

        reloader.Ignore(PathOf("hello.txt"));
        reloader.Notify(PathOf("hello.txt")); // the watcher reports the game's own write
        Wait(1);

        Assert.Equal(0, reloader.Poll());
        Assert.Equal("v1", _assets.Get(handle).Text);
    }

    [Fact]
    public void The_ignore_expires_so_a_later_outside_edit_is_picked_up()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        var reloader = CreateReloader();
        reloader.Ignore(PathOf("hello.txt"));
        Wait(AssetHotReloader.IgnoreSeconds + 1);
        Write("hello.txt", "edited outside");

        reloader.Notify(PathOf("hello.txt"));
        Wait(1);

        Assert.Equal(1, reloader.Poll());
        Assert.Equal("edited outside", _assets.Get(handle).Text);
    }

    [Fact]
    public void A_broken_file_is_reported_and_the_old_asset_is_kept()
    {
        var manager = new AssetManager(_root);
        var loader = new CountingLoader();
        manager.Register(loader);
        var handle = manager.Load<DisposableAsset>("hello.txt");
        var old = manager.Get(handle);
        var reloader = new AssetHotReloader(manager, _root, _log, () => _now);
        loader.Fail = true;

        reloader.Notify(PathOf("hello.txt"));
        Wait(1);
        reloader.Poll();

        Assert.Same(old, manager.Get(handle));
        Assert.Contains(Messages(), m => m.Contains("Could not reload hello.txt") && m.Contains("corrupt"));
        Assert.Equal(0, reloader.PendingCount); // not retried: a corrupt file will not fix itself
    }

    [Fact]
    public void A_locked_file_is_retried_and_then_loads()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");
        var reloader = CreateReloader();
        Write("hello.txt", "v2");

        using (var locked = new FileStream(PathOf("hello.txt"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            reloader.Notify(PathOf("hello.txt"));
            Wait(1);
            Assert.Equal(0, reloader.Poll());
            Assert.Equal(1, reloader.PendingCount); // queued again
        }

        Wait(1);
        Assert.Equal(1, reloader.Poll());
        Assert.Equal("v2", _assets.Get(handle).Text);
    }

    [Fact]
    public void A_file_that_stays_locked_is_given_up_on_after_the_retry_limit()
    {
        _assets.Load<TextAsset>("hello.txt");
        var reloader = CreateReloader();

        using var locked = new FileStream(PathOf("hello.txt"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        reloader.Notify(PathOf("hello.txt"));
        for (int i = 0; i < AssetHotReloader.MaxAttempts + 1; i++)
        {
            Wait(1);
            reloader.Poll();
        }

        Assert.Equal(0, reloader.PendingCount);
        Assert.Contains(Messages(), m => m.Contains("Could not reload hello.txt"));
    }

    [Fact]
    public void Starting_to_watch_a_missing_folder_fails_loudly()
    {
        var reloader = new AssetHotReloader(_assets, Path.Combine(_root, "nope"), _log);

        Assert.Throws<DirectoryNotFoundException>(() => reloader.Start());
    }

    [Fact]
    public void The_real_watcher_starts_and_stops()
    {
        var reloader = CreateReloader();

        reloader.Start();
        Assert.True(reloader.Watching);

        reloader.Dispose();
        Assert.False(reloader.Watching);
    }
}
