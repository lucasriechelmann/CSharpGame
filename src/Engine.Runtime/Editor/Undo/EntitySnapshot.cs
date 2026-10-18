using Engine.Core.Ecs;

namespace Engine.Runtime.Editor.Undo;

/// <summary>A copy of every component of one entity, enough to recreate it exactly (what delete-undo and redo-create use).</summary>
public sealed class EntitySnapshot
{
    private readonly List<(Type Type, object Value)> _components = new();

    /// <summary>Copies all components of <paramref name="entity"/> by value.</summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="entity">A live entity.</param>
    public static EntitySnapshot Capture(World world, Entity entity)
    {
        var types = new List<Type>();
        world.GetComponentTypes(entity, types);

        var snapshot = new EntitySnapshot();
        foreach (var type in types) snapshot._components.Add((type, world.GetBoxed(entity, type)));
        return snapshot;
    }

    /// <summary>Number of components held.</summary>
    public int Count => _components.Count;

    /// <summary>Creates a new entity with copies of the held components.</summary>
    /// <param name="world">The world to create it in.</param>
    public Entity Restore(World world)
    {
        var entity = world.CreateEntity();
        foreach (var (type, value) in _components) world.AddBoxed(entity, type, value);
        return entity;
    }
}
