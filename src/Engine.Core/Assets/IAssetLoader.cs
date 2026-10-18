namespace Engine.Core.Assets;

/// <summary>Knows how to turn a file on disk into an asset of type <typeparamref name="T"/>. Register one per asset type with <see cref="AssetManager.Register{T}"/>.</summary>
/// <typeparam name="T">The asset type produced.</typeparam>
public interface IAssetLoader<out T>
{
    /// <summary>Reads and builds the asset.</summary>
    /// <param name="fullPath">Absolute path of an existing file.</param>
    T Load(string fullPath);
}
