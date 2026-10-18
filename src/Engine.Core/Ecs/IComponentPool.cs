namespace Engine.Core.Ecs;

/// <summary>
/// Type-erased view of a <see cref="ComponentPool{T}"/> so <see cref="World"/> can,
/// for example, strip every component from an entity without knowing the component types.
/// </summary>
internal interface IComponentPool
{
    /// <summary>Number of components stored.</summary>
    int Count { get; }

    /// <summary>True when the entity id has a component in this pool.</summary>
    bool Has(int entityId);

    /// <summary>Removes the entity's component if present; returns whether anything was removed.</summary>
    bool Remove(int entityId);

    /// <summary>Removes every component from the pool.</summary>
    void Clear();
}
