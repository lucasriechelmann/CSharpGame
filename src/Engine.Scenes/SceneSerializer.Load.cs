using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Core.Ecs;

namespace Engine.Scenes;

// Loading: JSON -> world. Everything is parsed before any entity is created, so a bad file changes nothing.
public sealed partial class SceneSerializer
{
    /// <summary>Adds the entities of a scene document to the world.</summary>
    /// <param name="json">The scene JSON text.</param>
    /// <returns>The created entities, in file order.</returns>
    /// <exception cref="InvalidOperationException">The document is malformed, uses an unknown component or asset, or has invalid component data. The world is left unchanged.</exception>
    public IReadOnlyList<Entity> Load(string json)
    {
        JsonObject root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject
                ?? throw new InvalidOperationException("A scene must be a JSON object.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Scene is not valid JSON: {ex.Message}", ex);
        }

        int version = root[VersionKey]?.GetValue<int>()
            ?? throw new InvalidOperationException($"Scene is missing '{VersionKey}'.");
        if (version < 1 || version > CurrentVersion)
            throw new InvalidOperationException($"Scene version {version} is not supported (this build reads up to {CurrentVersion}).");

        if (root[EntitiesKey] is not JsonArray entityNodes)
            throw new InvalidOperationException($"Scene is missing the '{EntitiesKey}' array.");

        // Phase 1: parse everything. Errors surface here, before the world is touched.
        var prepared = new List<PreparedEntity>(entityNodes.Count);
        foreach (var node in entityNodes)
        {
            if (node is not JsonObject entityJson)
                throw new InvalidOperationException("Every entry in 'entities' must be a JSON object.");
            prepared.Add(PrepareEntity(entityJson));
        }

        // Phase 2: create the entities.
        return prepared.Select(Create).ToList();
    }

    /// <summary>Loads a scene file. Relative paths are resolved against the asset root.</summary>
    /// <returns>The created entities, in file order.</returns>
    public IReadOnlyList<Entity> LoadFile(string path)
    {
        string fullPath = ResolvePath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Scene '{path}' not found.", fullPath);
        return Load(File.ReadAllText(fullPath));
    }

    /// <summary>An entity entry parsed and ready to create: what to add, and which prefab (if any) it was made from.</summary>
    private readonly record struct PreparedEntity(List<Action<World, Entity>> Appliers, string? Prefab);

    private Entity Create(PreparedEntity prepared)
    {
        var entity = _world.CreateEntity();
        foreach (var apply in prepared.Appliers) apply(_world, entity);

        // Remember the prefab so saving can write the entity as a prefab plus overrides instead of expanding it.
        if (prepared.Prefab != null) _world.Add(entity, new PrefabLink { Prefab = prepared.Prefab });
        return entity;
    }

    /// <summary>Resolves the entity's prefab chain and overrides, then parses each component into a ready-to-apply action.</summary>
    private PreparedEntity PrepareEntity(JsonObject entityJson)
    {
        var components = ResolveComponents(entityJson, depth: 0);

        var appliers = new List<Action<World, Entity>>(components.Count);
        foreach (var (name, node) in components)
            appliers.Add(_registry.Get(name).Prepare(node!, _options));

        string? prefab = entityJson[PrefabKey]?.GetValue<string>();
        return new PreparedEntity(appliers, prefab);
    }
}
