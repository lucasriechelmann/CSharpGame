using System.Text.Json.Nodes;
using Engine.Core.Ecs;

namespace Engine.Scenes;

// Prefab editing: turning a live entity into a prefab and putting an instance back to what its prefab says.
public sealed partial class SceneSerializer
{
    /// <summary>
    /// Writes an entity as a prefab file and links the entity to it, so the entity becomes the first instance: saving the scene then
    /// writes it as the prefab name plus only what is changed later.
    /// </summary>
    /// <param name="entity">The entity to turn into a prefab.</param>
    /// <param name="prefabName">Where to write it, relative to the asset root, such as <c>prefabs/coin.prefab.json</c>.</param>
    /// <exception cref="ArgumentException">The name is empty.</exception>
    /// <exception cref="InvalidOperationException">The entity is not alive.</exception>
    public void SaveAsPrefab(Entity entity, string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName)) throw new ArgumentException("A prefab needs a file name.", nameof(prefabName));

        string fullPath = ResolvePath(prefabName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, SerializeEntity(entity).ToJsonString(_options));
        _world.Set(entity, new PrefabLink { Prefab = prefabName });
    }

    /// <summary>
    /// Puts an instance back to exactly what its prefab describes: every component the prefab has is overwritten with the prefab's value
    /// and every registered component the prefab does not have is removed. The prefab link itself stays.
    /// </summary>
    /// <param name="entity">An entity carrying a <see cref="PrefabLink"/>.</param>
    /// <exception cref="InvalidOperationException">The entity is not alive or has no prefab link, or the prefab is invalid.</exception>
    public void RevertToPrefab(Entity entity)
    {
        if (!_world.IsAlive(entity)) throw new InvalidOperationException($"{entity} is not alive.");
        if (!_world.TryGet<PrefabLink>(entity, out var link) || string.IsNullOrEmpty(link.Prefab))
            throw new InvalidOperationException($"{entity} was not made from a prefab.");

        // Parse everything first so a broken prefab leaves the entity untouched.
        var resolved = ResolveComponents(new JsonObject { [PrefabKey] = link.Prefab }, depth: 0);
        var appliers = new List<Action<World, Entity>>(resolved.Count);
        foreach (var (name, node) in resolved) appliers.Add(_registry.Get(name).Prepare(node!, _options));

        foreach (var serializer in _registry.Serializers)
        {
            if (!resolved.ContainsKey(serializer.Name)) _world.Remove(entity, serializer.Type);
        }

        foreach (var apply in appliers) apply(_world, entity);
    }
}
