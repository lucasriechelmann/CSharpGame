namespace Engine.Core.Assets;

/// <summary>Type-erased view of an <see cref="AssetStore{T}"/> so the manager can count and dispose every store without knowing asset types.</summary>
internal interface IAssetStore
{
    /// <summary>Number of assets held.</summary>
    int Count { get; }

    /// <summary>Finds the id of a stored asset by name.</summary>
    bool TryGetId(string name, out int id);

    /// <summary>
    /// Loads <paramref name="fullPath"/> again with <paramref name="loader"/> and swaps the result in under the existing handle.
    /// Returns false when no asset of this type has that name.
    /// </summary>
    bool Reload(string name, object loader, string fullPath);

    /// <summary>Disposes every asset that implements <see cref="IDisposable"/> and empties the store.</summary>
    void DisposeAll();
}
