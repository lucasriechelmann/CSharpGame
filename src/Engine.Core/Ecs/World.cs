namespace Engine.Core.Ecs;

/// <summary>
/// Container for entities and their components. Components are plain data (usually structs);
/// behaviour lives in systems that query the world.
/// </summary>
/// <remarks>
/// Split across partial files by topic: this file (entity lifetime), <c>World.Components.cs</c>,
/// <c>World.Queries.cs</c> and <c>World.Internals.cs</c> (pool lookup and guards).
/// </remarks>
public sealed partial class World
{
    private readonly EntityAllocator _allocator = new();
    private readonly Dictionary<Type, IComponentPool> _poolsByType = new();
    private readonly List<IComponentPool> _pools = new();

    /// <summary>Depth of running queries; structural changes are rejected while it is above zero.</summary>
    private int _iterating;

    /// <summary>Number of entities currently alive.</summary>
    public int EntityCount => _allocator.AliveCount;

    /// <summary>Creates a new, empty entity.</summary>
    /// <exception cref="InvalidOperationException">Called during a query.</exception>
    public Entity CreateEntity()
    {
        ThrowIfIterating();
        return _allocator.Create();
    }

    /// <summary>True when the handle refers to an entity that has not been destroyed.</summary>
    public bool IsAlive(Entity entity) => _allocator.IsAlive(entity);

    /// <summary>Destroys the entity and all of its components. Every outstanding handle to it goes stale.</summary>
    /// <exception cref="InvalidOperationException">The entity is dead, or this is called during a query.</exception>
    public void DestroyEntity(Entity entity)
    {
        ThrowIfIterating();
        ThrowIfDead(entity);

        foreach (var pool in _pools) pool.Remove(entity.Id);

        _allocator.Destroy(entity);
    }
}
