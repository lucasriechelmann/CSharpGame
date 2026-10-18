using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Runtime.Editor.Undo;

/// <summary>A copy of every component of one entity, enough to recreate it exactly (what delete-undo and redo-create use).</summary>
/// <remarks>
/// A <see cref="Parent"/> link holds an <see cref="Entity"/> handle, which goes stale when the parent is deleted and recreated by undo,
/// so it is not copied as a component. With a tracker the snapshot remembers the parent's tracked id instead and re-links to whatever
/// entity that id points at when it is restored.
/// </remarks>
public sealed class EntitySnapshot
{
    private readonly List<(Type Type, object Value)> _components = new();
    private int _parentId;

    /// <summary>Copies all components of <paramref name="entity"/> by value.</summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="entity">A live entity.</param>
    /// <param name="tracker">When given, a live <see cref="Parent"/> is remembered by tracked id so it can be re-linked on restore; without it the link is dropped.</param>
    public static EntitySnapshot Capture(World world, Entity entity, EntityTracker? tracker = null)
    {
        var types = new List<Type>();
        world.GetComponentTypes(entity, types);

        var snapshot = new EntitySnapshot();
        foreach (var type in types)
        {
            if (type == typeof(Parent)) continue;
            snapshot._components.Add((type, world.GetBoxed(entity, type)));
        }

        if (tracker != null && world.TryGet<Parent>(entity, out var parent) && world.IsAlive(parent.Value))
            snapshot._parentId = tracker.IdOf(parent.Value);
        return snapshot;
    }

    /// <summary>Number of components held (the parent link is not counted).</summary>
    public int Count => _components.Count;

    /// <summary>Creates a new entity with copies of the held components.</summary>
    /// <param name="world">The world to create it in.</param>
    /// <param name="tracker">Resolves the remembered parent id; required to restore a parent link.</param>
    /// <exception cref="InvalidOperationException">The snapshot has a parent but the tracker does not know a live entity for it (restore parents before children).</exception>
    public Entity Restore(World world, EntityTracker? tracker = null)
    {
        var parent = Entity.Null;
        if (_parentId != 0)
        {
            parent = tracker?.Lookup(_parentId) ?? Entity.Null;
            if (!world.IsAlive(parent))
                throw new InvalidOperationException($"Cannot restore the entity: its parent (tracked id {_parentId}) does not exist now.");
        }

        var entity = world.CreateEntity();
        foreach (var (type, value) in _components) world.AddBoxed(entity, type, value);
        if (!parent.IsNull) world.Add(entity, new Parent { Value = parent });
        return entity;
    }
}
