namespace Engine.Core.Ecs;

// Type-erased component access for tools (the inspector). These box and allocate, so they are for debug UI, never per-frame game code.
public sealed partial class World
{
    /// <summary>Appends the type of every component the entity has to <paramref name="types"/> (the caller owns and clears the list).</summary>
    /// <exception cref="InvalidOperationException">The entity is dead.</exception>
    public void GetComponentTypes(Entity entity, List<Type> types)
    {
        ThrowIfDead(entity);
        foreach (var pool in _pools)
            if (pool.Has(entity.Id)) types.Add(pool.ComponentType);
    }

    /// <summary>True when the entity is alive and has a component of the given runtime type.</summary>
    public bool Has(Entity entity, Type componentType) =>
        IsAlive(entity) && _poolsByType.TryGetValue(componentType, out var pool) && pool.Has(entity.Id);

    /// <summary>Copies the entity's component into a box.</summary>
    /// <exception cref="InvalidOperationException">The entity is dead or lacks the component.</exception>
    public object GetBoxed(Entity entity, Type componentType)
    {
        ThrowIfDead(entity);
        if (!_poolsByType.TryGetValue(componentType, out var pool) || !pool.Has(entity.Id))
            throw new InvalidOperationException($"{entity} has no component {componentType.Name}.");
        return pool.GetBoxed(entity.Id);
    }

    /// <summary>Overwrites an existing component from a box (allowed during queries, like <see cref="Set{T}"/> on an existing component).</summary>
    /// <exception cref="InvalidOperationException">The entity is dead or lacks the component.</exception>
    /// <exception cref="ArgumentException">The box is not a <paramref name="componentType"/>.</exception>
    public void SetBoxed(Entity entity, Type componentType, object value)
    {
        ThrowIfDead(entity);
        if (!componentType.IsInstanceOfType(value))
            throw new ArgumentException($"Expected a {componentType.Name} but got {value.GetType().Name}.", nameof(value));
        if (!_poolsByType.TryGetValue(componentType, out var pool) || !pool.Has(entity.Id))
            throw new InvalidOperationException($"{entity} has no component {componentType.Name}.");
        pool.SetBoxed(entity.Id, value);
    }

    /// <summary>Adds a component by runtime type, taking its value from a box (or a default value when <paramref name="value"/> is null).</summary>
    /// <exception cref="InvalidOperationException">The entity is dead, already has the component, or a query is running.</exception>
    /// <exception cref="ArgumentException">The box is not a <paramref name="componentType"/>.</exception>
    public void AddBoxed(Entity entity, Type componentType, object? value = null)
    {
        ThrowIfIterating();
        ThrowIfDead(entity);
        if (value != null && !componentType.IsInstanceOfType(value))
            throw new ArgumentException($"Expected a {componentType.Name} but got {value.GetType().Name}.", nameof(value));

        if (!_poolsByType.TryGetValue(componentType, out var pool))
        {
            pool = (IComponentPool)Activator.CreateInstance(typeof(ComponentPool<>).MakeGenericType(componentType))!;
            _poolsByType[componentType] = pool;
            _pools.Add(pool);
        }

        if (pool.Has(entity.Id))
            throw new InvalidOperationException($"{entity} already has component {componentType.Name}.");

        pool.AddDefault(entity.Id);
        if (value != null) pool.SetBoxed(entity.Id, value);
    }

    /// <summary>Removes a component by runtime type.</summary>
    /// <returns>True if the component existed and was removed.</returns>
    /// <exception cref="InvalidOperationException">The entity is dead, or a query is running.</exception>
    public bool Remove(Entity entity, Type componentType)
    {
        ThrowIfIterating();
        ThrowIfDead(entity);
        return _poolsByType.TryGetValue(componentType, out var pool) && pool.Remove(entity.Id);
    }
}
