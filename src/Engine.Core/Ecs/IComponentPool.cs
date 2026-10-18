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

    /// <summary>The component type stored in this pool.</summary>
    Type ComponentType { get; }

    /// <summary>Copies the entity's component into a box. The caller guarantees it exists. Allocates; for tools, not per-frame code.</summary>
    object GetBoxed(int entityId);

    /// <summary>Overwrites the entity's existing component from a box. The caller guarantees it exists and the box has the right type.</summary>
    void SetBoxed(int entityId, object value);

    /// <summary>Adds a default-initialised component for the entity. The caller guarantees it is not already present.</summary>
    void AddDefault(int entityId);
}
