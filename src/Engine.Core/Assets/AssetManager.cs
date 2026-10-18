namespace Engine.Core.Assets;

/// <summary>
/// Loads assets from disk through registered <see cref="IAssetLoader{T}"/>s, caches them by path so each file is
/// loaded once, and hands out <see cref="AssetHandle{T}"/>s. Disposing the manager disposes every disposable asset
/// it owns, so GPU resources are released in one place.
/// </summary>
public sealed class AssetManager : IDisposable
{
    private readonly Dictionary<Type, IAssetStore> _stores = new();
    private readonly Dictionary<Type, object> _loaders = new();

    /// <summary>Creates a manager that resolves relative asset paths against <paramref name="rootDirectory"/>.</summary>
    /// <param name="rootDirectory">Folder that contains the asset files.</param>
    public AssetManager(string rootDirectory)
    {
        RootDirectory = rootDirectory;
    }

    /// <summary>Folder that relative asset paths are resolved against.</summary>
    public string RootDirectory { get; }

    /// <summary>Total number of assets held across all types.</summary>
    public int Count => _stores.Values.Sum(store => store.Count);

    /// <summary>Teaches the manager how to load assets of type <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidOperationException">A loader for this type is already registered.</exception>
    public void Register<T>(IAssetLoader<T> loader)
    {
        if (!_loaders.TryAdd(typeof(T), loader))
            throw new InvalidOperationException($"A loader for {typeof(T).Name} is already registered.");
    }

    /// <summary>
    /// Loads the file at <paramref name="path"/> (relative to <see cref="RootDirectory"/>), or returns the existing
    /// handle if it was loaded before. Slash direction does not matter.
    /// </summary>
    /// <exception cref="InvalidOperationException">No loader is registered for <typeparamref name="T"/>.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public AssetHandle<T> Load<T>(string path)
    {
        string name = NormalizeName(path);
        var store = GetOrCreateStore<T>();
        if (store.TryGetId(name, out int existing)) return new AssetHandle<T>(existing);

        if (!_loaders.TryGetValue(typeof(T), out var loader))
            throw new InvalidOperationException($"No loader is registered for {typeof(T).Name}.");

        string fullPath = Path.GetFullPath(Path.Combine(RootDirectory, name));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Asset '{name}' ({typeof(T).Name}) not found.", fullPath);

        return new AssetHandle<T>(store.Add(name, ((IAssetLoader<T>)loader).Load(fullPath)));
    }

    /// <summary>
    /// Adds an asset created in code (for example a procedural texture) under a unique name. The manager takes
    /// ownership and will dispose it.
    /// </summary>
    /// <exception cref="InvalidOperationException">An asset with that name and type already exists.</exception>
    public AssetHandle<T> Add<T>(string name, T asset) =>
        new(GetOrCreateStore<T>().Add(NormalizeName(name), asset));

    /// <summary>Returns the asset behind a handle.</summary>
    /// <exception cref="KeyNotFoundException">The handle is invalid or does not belong to this manager.</exception>
    public T Get<T>(AssetHandle<T> handle)
    {
        if (_stores.TryGetValue(typeof(T), out var raw) && raw is AssetStore<T> store && store.Contains(handle.Id))
            return store.Get(handle.Id);

        throw new KeyNotFoundException($"Invalid or unknown {typeof(T).Name} handle (id {handle.Id}).");
    }

    /// <summary>Returns the asset behind a handle, or false if the handle is not valid here.</summary>
    public bool TryGet<T>(AssetHandle<T> handle, out T asset)
    {
        if (_stores.TryGetValue(typeof(T), out var raw) && raw is AssetStore<T> store && store.Contains(handle.Id))
        {
            asset = store.Get(handle.Id);
            return true;
        }

        asset = default!;
        return false;
    }

    /// <summary>The stable name (normalised relative path) a handle was loaded or added under; this is what scenes should save.</summary>
    /// <exception cref="KeyNotFoundException">The handle is invalid or does not belong to this manager.</exception>
    public string NameOf<T>(AssetHandle<T> handle)
    {
        if (_stores.TryGetValue(typeof(T), out var raw) && raw is AssetStore<T> store && store.Contains(handle.Id))
            return store.NameOf(handle.Id);

        throw new KeyNotFoundException($"Invalid or unknown {typeof(T).Name} handle (id {handle.Id}).");
    }

    /// <summary>Disposes every disposable asset and clears all stores. Handles become invalid.</summary>
    public void Dispose()
    {
        foreach (var store in _stores.Values) store.DisposeAll();
        _stores.Clear();
    }

    private static string NormalizeName(string path)
    {
        string name = path.Replace('\\', '/');
        while (name.StartsWith("./", StringComparison.Ordinal)) name = name[2..];
        return name;
    }

    private AssetStore<T> GetOrCreateStore<T>()
    {
        if (_stores.TryGetValue(typeof(T), out var existing)) return (AssetStore<T>)existing;

        var store = new AssetStore<T>();
        _stores[typeof(T)] = store;
        return store;
    }
}
