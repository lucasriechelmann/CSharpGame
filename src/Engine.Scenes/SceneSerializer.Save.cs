using System.Text.Json.Nodes;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Scenes;

// Saving: world -> JSON.
public sealed partial class SceneSerializer
{
    /// <summary>
    /// Serializes every entity and its registered components into a scene document. An entity that was created from a prefab
    /// (it carries a <see cref="PrefabLink"/>) is written as the prefab name plus only the components and fields that differ from
    /// the prefab, so a scene of prefab instances stays small and keeps following its prefabs; any other entity is written in full.
    /// </summary>
    /// <returns>Indented JSON text.</returns>
    public string Save()
    {
        var prefabBases = new Dictionary<string, JsonObject>();
        var entities = new JsonArray();
        var indices = new Dictionary<Entity, int>();
        foreach (var entity in _world.Entities) indices[entity] = indices.Count;

        foreach (var entity in _world.Entities)
        {
            var entry = SerializeForScene(entity, prefabBases);

            // The link is written as the parent's position in this file, because a handle means nothing after a reload.
            var parent = TransformHierarchy.GetParent(_world, entity);
            if (!parent.IsNull) entry[ParentKey] = indices[parent];
            entities.Add(entry);
        }

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

        return new JsonObject { [ComponentsKey] = WriteComponents(_world, entity) };
    }

    /// <summary>One entity as a scene entry: prefab plus overrides when it has a link, otherwise every component.</summary>
    private JsonObject SerializeForScene(Entity entity, Dictionary<string, JsonObject> prefabBases)
    {
        if (!_world.TryGet<PrefabLink>(entity, out var link) || string.IsNullOrEmpty(link.Prefab))
            return SerializeEntity(entity);

        // The base is what the prefab alone produces, written through the same serializers as the entity, so equal values
        // are formatted alike and only real differences end up in the patch.
        if (!prefabBases.TryGetValue(link.Prefab, out var prefabComponents))
        {
            prefabComponents = WriteComponentsOfPrefab(link.Prefab);
            prefabBases[link.Prefab] = prefabComponents;
        }

        var overrides = JsonMerge.Diff(prefabComponents, WriteComponents(_world, entity));
        var entry = new JsonObject { [PrefabKey] = link.Prefab };
        if (overrides.Count > 0) entry[ComponentsKey] = overrides;
        return entry;
    }

    /// <summary>Builds the prefab in a scratch world and writes its components, to compare instances against.</summary>
    private JsonObject WriteComponentsOfPrefab(string prefab)
    {
        var resolved = ResolveComponents(new JsonObject { [PrefabKey] = prefab }, depth: 0);
        var scratch = new World();
        var entity = scratch.CreateEntity();
        foreach (var (name, node) in resolved)
            _registry.Get(name).Prepare(node!, _options)(scratch, entity);

        return WriteComponents(scratch, entity);
    }

    private JsonObject WriteComponents(World world, Entity entity)
    {
        var components = new JsonObject();
        foreach (var serializer in _registry.Serializers)
        {
            if (serializer.TryWrite(world, entity, _options, out var node))
                components[serializer.Name] = node;
        }

        return components;
    }
}
