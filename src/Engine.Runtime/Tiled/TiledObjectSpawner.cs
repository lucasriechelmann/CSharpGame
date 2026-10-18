using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Tilemaps.Tiled;

namespace Engine.Runtime.Tiled;

/// <summary>
/// Turns the objects of an imported Tiled map into entities made from prefabs: the level designer places a "Coin" point in Tiled, the game says that
/// class means <c>prefabs/coin.prefab.json</c>, and the coin appears. It is the data-driven half of loading a Tiled level; tile layers are spawned
/// by <see cref="TiledSpawner"/>.
/// </summary>
/// <remarks>
/// It takes the instantiate function as a delegate instead of a <c>SceneSerializer</c> so it can be tested without files; a game passes
/// <c>Scenes.Instantiate</c>. Prefab components keep their own Z (draw depth); Tiled only decides X and Y.
/// </remarks>
public static class TiledObjectSpawner
{
    /// <summary>Instantiates a prefab for every object whose class is a key of <paramref name="prefabByClass"/>.</summary>
    /// <param name="world">The world the entities live in (to place them).</param>
    /// <param name="instantiate">Creates an entity from a prefab name, for example <c>Scenes.Instantiate</c>.</param>
    /// <param name="map">The imported map.</param>
    /// <param name="prefabByClass">Object class (Tiled's "Class" field) to prefab file name.</param>
    /// <param name="offset">Added to every position, to place the map somewhere in the world.</param>
    public static TiledSpawnResult Spawn(World world, Func<string, Entity> instantiate, ImportedMap map, IReadOnlyDictionary<string, string> prefabByClass, Vector2 offset)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(instantiate);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(prefabByClass);

        var spawned = new List<Entity>();
        var unmapped = new List<ImportedObject>();
        foreach (var obj in map.Objects)
        {
            if (!prefabByClass.TryGetValue(obj.Class, out string? prefab))
            {
                unmapped.Add(obj);
                continue;
            }

            var entity = instantiate(prefab);
            spawned.Add(entity);
            Place(world, entity, obj, offset);
        }

        return new TiledSpawnResult(spawned, unmapped);
    }

    /// <summary>Puts the entity at the object's centre (a point is its own centre) and gives it the object's name when it has one.</summary>
    private static void Place(World world, Entity entity, ImportedObject obj, Vector2 offset)
    {
        if (world.TryGet<Transform>(entity, out var transform))
        {
            var centre = obj.Position + obj.Size / 2f + offset;
            transform.Position = new Vector3(centre.X, centre.Y, transform.Position.Z);
            world.Set(entity, transform);
        }

        if (!string.IsNullOrEmpty(obj.Name)) world.Set(entity, Name.Of(obj.Name));
    }
}
