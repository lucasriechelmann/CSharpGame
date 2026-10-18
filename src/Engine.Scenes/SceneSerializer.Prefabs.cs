using System.Text.Json.Nodes;
using Engine.Core.Ecs;

namespace Engine.Scenes;

// Prefabs: template lookup, inheritance and instantiation.
public sealed partial class SceneSerializer
{
    /// <summary>Deepest allowed chain of prefabs based on prefabs. Hitting it almost certainly means a cycle.</summary>
    private const int MaxPrefabDepth = 16;

    /// <summary>Creates a new entity from a prefab file (a name relative to the asset root, for example <c>prefabs/hero.prefab.json</c>).</summary>
    /// <exception cref="InvalidOperationException">The prefab is missing or invalid, or uses an unknown component.</exception>
    public Entity Instantiate(string prefab) => Instantiate(new JsonObject { [PrefabKey] = prefab });

    /// <summary>
    /// Creates a new entity from entity JSON, which may name a <c>prefab</c> and override its <c>components</c> by merge.
    /// </summary>
    public Entity Instantiate(JsonObject entityJson) => Create(PrepareEntity(entityJson));

    /// <summary>
    /// Returns the final components of an entity entry: the prefab's components (recursively resolved), with this entry's
    /// own <c>components</c> merged on top. Always works on copies, so cached prefabs are never modified.
    /// </summary>
    private JsonObject ResolveComponents(JsonObject entityJson, int depth)
    {
        if (depth > MaxPrefabDepth)
            throw new InvalidOperationException($"Prefabs are nested more than {MaxPrefabDepth} deep; is there a cycle?");

        foreach (var (key, _) in entityJson)
        {
            // 'parent' is a scene-level link to another entry, so it means something only on the entry itself, not inside a prefab.
            if (key != PrefabKey && key != ComponentsKey && !(key == ParentKey && depth == 0))
                throw new InvalidOperationException($"Unknown key '{key}' in an entity (expected '{PrefabKey}' or '{ComponentsKey}').");
        }

        var components = new JsonObject();

        if (entityJson[PrefabKey] is { } prefabNode)
        {
            string prefabName = prefabNode.GetValue<string>();
            var prefab = _assets.Get(_assets.Load<Prefab>(prefabName));
            JsonMerge.Apply(components, ResolveComponents(prefab.Template, depth + 1));
        }

        if (entityJson[ComponentsKey] is JsonObject own)
            JsonMerge.Apply(components, own);
        else if (entityJson[ComponentsKey] is not null)
            throw new InvalidOperationException($"'{ComponentsKey}' must be a JSON object.");

        return components;
    }
}
