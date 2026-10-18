using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Tilemaps;

namespace Engine.Runtime.Editor.Undo;

/// <summary>What commands act on: the world, the tracker that maps tracked ids to live entities, and the shared write helpers.</summary>
/// <param name="world">The world being edited.</param>
public sealed class EditContext(World world)
{
    /// <summary>The world being edited.</summary>
    public World World { get; } = world;

    /// <summary>Tile layers painted since they were last written to disk. Save writes these files.</summary>
    public HashSet<AssetHandle<TileLayer>> DirtyTileLayers { get; } = new();

    /// <summary>Maps stable ids to the entities that currently exist.</summary>
    public EntityTracker Entities { get; } = new();

    /// <summary>The live entity for a tracked id.</summary>
    /// <param name="id">A tracked id from <see cref="EntityTracker.IdOf"/>.</param>
    /// <exception cref="InvalidOperationException">The entity does not exist now (the history is out of step with the world).</exception>
    public Entity Resolve(int id)
    {
        var entity = Entities.Lookup(id);
        if (entity.IsNull || !World.IsAlive(entity))
            throw new InvalidOperationException($"Tracked entity {id} does not exist; the undo history no longer matches the world.");
        return entity;
    }

    /// <summary>The live entity for a tracked id, or <see cref="Entity.Null"/> when it does not exist now.</summary>
    /// <param name="id">A tracked id.</param>
    public Entity ResolveOrNull(int id)
    {
        var entity = Entities.Lookup(id);
        return !entity.IsNull && World.IsAlive(entity) ? entity : Entity.Null;
    }

    /// <summary>
    /// Overwrites a component. Writing a <see cref="Transform"/> also clears the entity's <see cref="PreviousTransform"/>, because
    /// the simulation is frozen while editing and an old history would leave the entity drawn part-way back to where it was.
    /// </summary>
    /// <param name="entity">The entity to change.</param>
    /// <param name="type">The component type.</param>
    /// <param name="value">The new boxed value.</param>
    public void SetComponent(Entity entity, Type type, object value)
    {
        World.SetBoxed(entity, type, value);
        if (type == typeof(Transform) && World.Has<PreviousTransform>(entity))
            World.Get<PreviousTransform>(entity) = default;
    }
}
