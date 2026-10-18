using Engine.Core.Logging;

namespace Engine.Core.Assets;

/// <summary>
/// Watches a folder and reloads loaded assets whose files change, so a texture, tile layer, prefab or animation edited in another
/// program shows up in the running game. File events arrive on a worker thread and are only queued; <see cref="Poll"/> does the actual
/// reloading and must be called from the thread that owns the GPU context (the game loop), because reloading a texture calls OpenGL.
/// </summary>
/// <remarks>
/// Editors write a file in several steps and fire several events, so a change is acted on only once it has been quiet for
/// <see cref="QuietSeconds"/>, and a file that is still locked is retried a few times. Files the game wrote itself (the editor saving a
/// tile layer) are skipped through <see cref="Ignore"/>. Only assets that are already loaded are reloaded; other files are ignored.
/// What a reload cannot do: objects that were built from the old asset and kept (an animation machine copied into a system, a collision
/// grid built from a tile layer) are not rebuilt.
/// </remarks>
public sealed class AssetHotReloader : IDisposable
{
    /// <summary>How long a file must stay unchanged before it is reloaded.</summary>
    public const double QuietSeconds = 0.3;

    /// <summary>How long a path that the game wrote itself is ignored after <see cref="Ignore"/>.</summary>
    public const double IgnoreSeconds = 2.0;

    /// <summary>How many times a file that fails with an I/O error (still being written) is retried before giving up.</summary>
    public const int MaxAttempts = 5;

    private readonly AssetManager _assets;
    private readonly string _root;
    private readonly Logger _log;
    private readonly Func<DateTime> _clock;
    private readonly Dictionary<string, Pending> _pending = new();
    private readonly Dictionary<string, DateTime> _ignoredUntil = new();
    private readonly object _lock = new();
    private FileSystemWatcher? _watcher;

    /// <summary>Creates a reloader; call <see cref="Start"/> to begin watching.</summary>
    /// <param name="assets">The manager whose assets are reloaded.</param>
    /// <param name="watchRoot">The folder to watch. Names are taken relative to it, so it must be laid out like the asset root.</param>
    /// <param name="log">Where reloads and failures are reported.</param>
    /// <param name="clock">Supplies the time (UTC); tests pass a fake.</param>
    public AssetHotReloader(AssetManager assets, string watchRoot, Logger log, Func<DateTime>? clock = null)
    {
        _assets = assets;
        _root = Path.GetFullPath(watchRoot);
        _log = log;
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    /// <summary>True while a watcher is running.</summary>
    public bool Watching => _watcher != null;

    /// <summary>Number of files waiting to be reloaded.</summary>
    public int PendingCount
    {
        get
        {
            lock (_lock) return _pending.Count;
        }
    }

    /// <summary>Starts watching the folder, including its subfolders.</summary>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    public void Start()
    {
        if (_watcher != null) return;
        if (!Directory.Exists(_root)) throw new DirectoryNotFoundException($"Cannot watch '{_root}': the folder does not exist.");

        var watcher = new FileSystemWatcher(_root) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size };
        watcher.Changed += (_, e) => Notify(e.FullPath);
        watcher.Created += (_, e) => Notify(e.FullPath);
        watcher.Renamed += (_, e) => Notify(e.FullPath);
        watcher.EnableRaisingEvents = true;
        _watcher = watcher;
    }

    /// <summary>Records that a file changed. Safe to call from any thread; this is what the file watcher calls.</summary>
    /// <param name="fullPath">Absolute path of the changed file.</param>
    public void Notify(string fullPath)
    {
        if (!TryGetName(fullPath, out string name)) return;

        lock (_lock) _pending[name] = new Pending(_clock(), 0);
    }

    /// <summary>Tells the reloader the game itself just wrote this file, so the change it will see should not trigger a reload.</summary>
    /// <param name="fullPath">Absolute path of the file that was written.</param>
    public void Ignore(string fullPath)
    {
        if (!TryGetName(fullPath, out string name)) return;

        lock (_lock)
        {
            _ignoredUntil[name] = _clock().AddSeconds(IgnoreSeconds);
            _pending.Remove(name);
        }
    }

    /// <summary>Reloads the files that have been quiet long enough. Call once per frame from the game thread.</summary>
    /// <returns>How many assets were reloaded.</returns>
    public int Poll()
    {
        List<(string Name, Pending Item)> due;
        var now = _clock();
        lock (_lock)
        {
            if (_pending.Count == 0) return 0;

            due = _pending.Where(p => (now - p.Value.LastEvent).TotalSeconds >= QuietSeconds).Select(p => (p.Key, p.Value)).ToList();
            foreach (var (name, _) in due) _pending.Remove(name);
        }

        int reloaded = 0;
        foreach (var (name, item) in due)
        {
            lock (_lock)
            {
                if (_ignoredUntil.TryGetValue(name, out var until))
                {
                    if (until > now) continue;
                    _ignoredUntil.Remove(name);
                }
            }

            reloaded += Reload(name, item);
        }

        return reloaded;
    }

    private int Reload(string name, Pending item)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_root, name));
        try
        {
            int count = _assets.Reload(name, fullPath);
            if (count > 0) _log.Info("Assets", $"Reloaded {name}");
            return count;
        }
        catch (IOException error) when (item.Attempts + 1 < MaxAttempts)
        {
            // Probably still being written by the program that saved it: try again after another quiet period.
            lock (_lock) _pending[name] = new Pending(_clock(), item.Attempts + 1);
            _log.Debug("Assets", $"{name} is busy ({error.Message}); will retry");
            return 0;
        }
        catch (Exception error)
        {
            // The old asset stays in place (the loader threw before anything was swapped).
            _log.Error("Assets", $"Could not reload {name}: {error.Message}");
            return 0;
        }
    }

    private bool TryGetName(string fullPath, out string name)
    {
        name = string.Empty;
        string relative = Path.GetRelativePath(_root, Path.GetFullPath(fullPath));
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) return false; // outside the watched folder

        name = relative.Replace('\\', '/');
        return true;
    }

    /// <summary>Stops watching.</summary>
    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    private readonly record struct Pending(DateTime LastEvent, int Attempts);
}
