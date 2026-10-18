using System.Text.Json.Nodes;
using Engine.Core.Ecs;

namespace Engine.Scenes;

// Saving: world -> JSON.
public sealed partial class SceneSerializer
{
    /// <summary>Serializes every entity and its registered components into a scene document.</summary>
    /// <returns>Indented JSON text.</returns>
    public string Save()
    {
        var entities = new JsonArray();
        foreach (var entity in _world.Entities)
            entities.Add(SerializeEntity(entity));

        var root = new JsonObject
        {
            [VersionKey] = CurrentVersion,
            [EntitiesKey] = entities,
        };
        return root.ToJsonString(_options);
    }

    /// <summary>Saves the scene to a file, creating folders as needed. Relative paths are resolved against the asset root.</summary>
    public void SaveToFile(string path)
    {
        string fullPath = ResolvePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, Save());
    }

    /// <summary>
    /// Serializes one entity as <c>{ "components": { ... } }</c>. The result is also a valid prefab, so this is how a prefab
    /// file can be authored from an entity built in code.
    /// </summary>
    /// <exception cref="InvalidOperationException">The entity is not alive.</exception>
    public JsonObject SerializeEntity(Entity entity)
    {
        if (!_world.IsAlive(entity)) throw new InvalidOperationException($"{entity} is not alive.");

        var components = new JsonObject();
        foreach (var serializer in _registry.Serializers)
        {
            if (serializer.TryWrite(_world, entity, _options, out var node))
                components[serializer.Name] = node;
        }

        return new JsonObject { [ComponentsKey] = components };
    }
}
