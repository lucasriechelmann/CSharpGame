namespace Engine.Core.Ecs;

// Component add / set / get / remove.
public sealed partial class World
{
    /// <summary>Adds a component to an entity.</summary>
    /// <returns>A reference to the stored component, valid until the pool next grows.</returns>
    /// <exception cref="InvalidOperationException">The entity is dead, already has this component, or a query is running.</exception>
    public ref T Add<T>(Entity entity, T value = default!)
    {
        ThrowIfIterating();
        ThrowIfDead(entity);
        var pool = GetOrCreatePool<T>();
        if (pool.Has(entity.Id))
            throw new InvalidOperationException($"{entity} already has component {typeof(T).Name}.");
        ref T slot = ref pool.Add(entity.Id);
        slot = value;
        return ref slot;
    }

    /// <summary>Adds the component, or overwrites it if the entity already has one. Overwriting is allowed during queries.</summary>
    /// <returns>A reference to the stored component.</returns>
    public ref T Set<T>(Entity entity, T value)
    {
        ThrowIfDead(entity);
        var pool = GetOrCreatePool<T>();
        if (pool.Has(entity.Id))
        {
            ref T existing = ref pool.Get(entity.Id);
            existing = value;
            return ref existing;
        }

        ThrowIfIterating();
        ref T slot = ref pool.Add(entity.Id);
        slot = value;
        return ref slot;
    }

    /// <summary>True when the entity is alive and has a component of type <typeparamref name="T"/>.</summary>
    public bool Has<T>(Entity entity) =>
        IsAlive(entity) && TryGetPool<T>(out var pool) && pool.Has(entity.Id);

    /// <summary>Returns a reference to the entity's component so it can be modified in place.</summary>
    /// <exception cref="InvalidOperationException">The entity is dead or lacks the component.</exception>
    public ref T Get<T>(Entity entity)
    {
        ThrowIfDead(entity);
        if (!TryGetPool<T>(out var pool) || !pool.Has(entity.Id))
            throw new InvalidOperationException($"{entity} has no component {typeof(T).Name}.");
        return ref pool.Get(entity.Id);
    }

    /// <summary>Copies the component out if present. Returns false (and a default value) otherwise.</summary>
    public bool TryGet<T>(Entity entity, out T value)
    {
        if (Has<T>(entity))
        {
            value = GetPool<T>().Get(entity.Id);
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Removes a component from an entity.</summary>
    /// <returns>True if the component existed and was removed.</returns>
    /// <exception cref="InvalidOperationException">The entity is dead, or a query is running.</exception>
    public bool Remove<T>(Entity entity)
    {
        ThrowIfIterating();
        ThrowIfDead(entity);
        return TryGetPool<T>(out var pool) && pool.Remove(entity.Id);
    }
}
