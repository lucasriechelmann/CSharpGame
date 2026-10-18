namespace Engine.Core.Assets;

/// <summary>
/// A lightweight, typed reference to an asset owned by an <see cref="AssetManager"/>. It is just an integer, so it is
/// cheap to copy, fine to store in components, and distinct per asset type (a handle to a texture cannot be used as a
/// handle to a sound). Ids are only meaningful within one manager and one run; use <see cref="AssetManager.NameOf{T}"/>
/// to get the stable name for saving.
/// </summary>
/// <typeparam name="T">The kind of asset referenced.</typeparam>
/// <param name="Id">1-based index in the manager's store for <typeparamref name="T"/>; 0 means "no asset".</param>
public readonly record struct AssetHandle<T>(int Id)
{
    /// <summary>The "no asset" handle (also what <c>default</c> gives you).</summary>
    public static AssetHandle<T> Invalid => default;

    /// <summary>True when the handle refers to an asset (it does not guarantee the asset is still loaded).</summary>
    public bool IsValid => Id > 0;
}
