using Engine.Core.Ecs;
using Engine.Tilemaps.Tiled;

namespace Engine.Runtime.Tiled;

/// <summary>What <see cref="TiledObjectSpawner"/> did with a map's objects.</summary>
/// <param name="Spawned">The entities it created, in the order of the objects they came from.</param>
/// <param name="Unmapped">The objects whose class had no prefab, left for the game to handle itself (a player start, an exit trigger).</param>
public sealed record TiledSpawnResult(IReadOnlyList<Entity> Spawned, IReadOnlyList<ImportedObject> Unmapped);
