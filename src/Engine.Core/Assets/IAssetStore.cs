namespace Engine.Core.Assets;

/// <summary>Type-erased view of an <see cref="AssetStore{T}"/> so the manager can count and dispose every store without knowing asset types.</summary>
internal interface IAssetStore
{
    /// <summary>Number of assets held.</summary>
    int Count { get; }

    /// <summary>Disposes every asset that implements <see cref="IDisposable"/> and empties the store.</summary>
    void DisposeAll();
}
