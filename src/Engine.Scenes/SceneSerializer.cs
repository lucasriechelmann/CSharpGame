using System.Text.Json;
using Engine.Core.Assets;
using Engine.Core.Ecs;

namespace Engine.Scenes;

/// <summary>
/// Saves a <see cref="World"/> to JSON and loads JSON scenes and prefabs into it. Components are written through the
/// <see cref="ComponentRegistry"/>, assets by name, and prefab overrides by JSON merge.
/// </summary>
/// <remarks>
/// <para>Scene file shape: <c>{ "version": 1, "entities": [ { "prefab": "optional/path.prefab.json", "parent": 0, "components": { "Transform": { ... } } } ] }</c>. <c>parent</c> is the index of another entry in the same array (the entity is then its child and its Transform is local); it is entity-level data, not a component, so no Entity handle is ever written.</para>
/// <para>Split by topic: this file (setup), <c>SceneSerializer.Save.cs</c>, <c>SceneSerializer.Load.cs</c> and <c>SceneSerializer.Prefabs.cs</c>.</para>
/// <para>Limitation: components that store an <see cref="Entity"/> reference are not remapped on load; avoid them in saved components for now.</para>
/// </remarks>
public sealed partial class SceneSerializer
{
    /// <summary>The scene file format version this serializer writes and the newest it can read.</summary>
    public const int CurrentVersion = 1;

    private const string VersionKey = "version";
    private const string EntitiesKey = "entities";
    private const string PrefabKey = "prefab";
    private const string ComponentsKey = "components";
    private const string ParentKey = "parent";

    private readonly World _world;
    private readonly AssetManager _assets;
    private readonly ComponentRegistry _registry;
    private readonly JsonSerializerOptions _options;

    /// <summary>Creates a serializer for one world.</summary>
    /// <param name="world">The world to save from and load into.</param>
    /// <param name="assets">Resolves asset names (textures, tile layers, prefabs, ...). Needs a <see cref="PrefabLoader"/> registered to use prefabs.</param>
    /// <param name="registry">The component types that may appear in files.</param>
    public SceneSerializer(World world, AssetManager assets, ComponentRegistry registry)
    {
        _world = world;
        _assets = assets;
        _registry = registry;
        _options = SceneJson.CreateOptions(assets);
    }

    private string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(_assets.RootDirectory, path);
}
