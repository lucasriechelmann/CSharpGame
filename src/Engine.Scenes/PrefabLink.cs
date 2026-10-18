namespace Engine.Scenes;

/// <summary>
/// Remembers which prefab an entity was created from, so saving can write <c>{ "prefab": ..., "components": { only what differs } }</c>
/// instead of expanding every component. Added by <see cref="SceneSerializer"/> when it creates an entity from a prefab; it is not
/// a registered component, so it never appears in scene files as one.
/// </summary>
public struct PrefabLink
{
    /// <summary>The prefab name as written in the file, relative to the asset root (for example <c>prefabs/coin.prefab.json</c>).</summary>
    public string Prefab;
}
