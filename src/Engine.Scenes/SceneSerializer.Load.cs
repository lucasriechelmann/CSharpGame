using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

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

        ValidateParents(prepared);

        // Phase 2: create the entities, then link children to parents (a parent may appear after its child in the file).
        var created = prepared.Select(Create).ToList();
        for (int i = 0; i < created.Count; i++)
        {
            if (prepared[i].ParentIndex != NoParent) _world.Add(created[i], new Parent { Value = created[prepared[i].ParentIndex] });
        }

        return created;
    }

    /// <summary>Checks every <c>parent</c> index before anything is created: in range, not the entity itself, and no loop.</summary>
    private static void ValidateParents(List<PreparedEntity> prepared)
    {
        for (int i = 0; i < prepared.Count; i++)
        {
            if (prepared[i].ParentIndex == NoParent) continue;
            if (prepared[i].ParentIndex < 0 || prepared[i].ParentIndex >= prepared.Count)
                throw new InvalidOperationException($"Entity {i} names parent {prepared[i].ParentIndex}, but the scene has {prepared.Count} entities.");
        }

        // With every index in range, a chain longer than the entity count must revisit someone, which is a loop.
        for (int i = 0; i < prepared.Count; i++)
        {
            int current = i;
            for (int steps = 0; current != NoParent; steps++)
            {
                if (steps > prepared.Count) throw new InvalidOperationException($"Entity {i} is its own ancestor (parent loop in the scene).");
                current = prepared[current].ParentIndex;
            }
        }
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
    private readonly record struct PreparedEntity(List<Action<World, Entity>> Appliers, string? Prefab, int ParentIndex = NoParent);

    private const int NoParent = -1;

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
        int parentIndex = entityJson[ParentKey]?.GetValue<int>() ?? NoParent;
        return new PreparedEntity(appliers, prefab, parentIndex);
    }
}
