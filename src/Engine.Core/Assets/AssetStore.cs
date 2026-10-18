namespace Engine.Core.Assets;

/// <summary>Holds all loaded assets of one type: a list indexed by handle id plus a name-to-id lookup for caching.</summary>
/// <typeparam name="T">The asset type stored.</typeparam>
internal sealed class AssetStore<T> : IAssetStore
{
    private readonly List<T> _assets = new();
    private readonly List<string> _names = new();
    private readonly Dictionary<string, int> _idsByName = new();

    /// <inheritdoc />
    public int Count => _assets.Count;

    /// <summary>Stores a new asset under a unique name and returns its 1-based id.</summary>
    public int Add(string name, T asset)
    {
        if (_idsByName.ContainsKey(name))
            throw new InvalidOperationException($"An asset named '{name}' of type {typeof(T).Name} already exists.");

        _assets.Add(asset);
        _names.Add(name);
        int id = _assets.Count;
        _idsByName[name] = id;
        return id;
    }

    /// <summary>Finds the id of a previously added asset by name.</summary>
    public bool TryGetId(string name, out int id) => _idsByName.TryGetValue(name, out id);

    /// <summary>True when the id refers to a stored asset.</summary>
    public bool Contains(int id) => id >= 1 && id <= _assets.Count;

    /// <summary>Returns the asset for a valid id.</summary>
    public T Get(int id) => _assets[id - 1];

    /// <summary>Returns the name an asset was stored under.</summary>
    public string NameOf(int id) => _names[id - 1];

    /// <inheritdoc />
    public void DisposeAll()
    {
        foreach (var asset in _assets)
            (asset as IDisposable)?.Dispose();

        _assets.Clear();
        _names.Clear();
        _idsByName.Clear();
    }
}
