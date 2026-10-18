namespace Engine.Core.Ecs;

public delegate void EntityAction<T1>(Entity entity, ref T1 c1);
public delegate void EntityAction<T1, T2>(Entity entity, ref T1 c1, ref T2 c2);
public delegate void EntityAction<T1, T2, T3>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3);

/// <summary>
/// Container for entities and their components. Components are plain data (usually structs);
/// behaviour lives in systems that query the world.
/// </summary>
public sealed class World
{
    private readonly List<int> _generations = new() { 0 }; // slot 0 is unused so Entity.Null never aliases a real entity
    private readonly Stack<int> _freeIds = new();
    private readonly Dictionary<Type, IComponentPool> _poolsByType = new();
    private readonly List<IComponentPool> _pools = new();
    private int _iterating;

    public int EntityCount { get; private set; }

    // ---- entities ----

    public Entity CreateEntity()
    {
        ThrowIfIterating();
        int id;
        if (_freeIds.Count > 0)
        {
            id = _freeIds.Pop();
        }
        else
        {
            id = _generations.Count;
            _generations.Add(1);
        }

        EntityCount++;
        return new Entity(id, _generations[id]);
    }

    public bool IsAlive(Entity entity) =>
        entity.Generation != 0
        && (uint)entity.Id < (uint)_generations.Count
        && _generations[entity.Id] == entity.Generation;

    public void DestroyEntity(Entity entity)
    {
        ThrowIfIterating();
        ThrowIfDead(entity);

        foreach (var pool in _pools) pool.Remove(entity.Id);

        _generations[entity.Id]++; // invalidates every outstanding handle to this slot
        _freeIds.Push(entity.Id);
        EntityCount--;
    }

    // ---- components ----

    /// <summary>Adds a component; throws if the entity already has one of this type.</summary>
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

    /// <summary>Adds the component or overwrites the existing one.</summary>
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

    public bool Has<T>(Entity entity) =>
        IsAlive(entity) && TryGetPool<T>(out var pool) && pool.Has(entity.Id);

    /// <summary>Returns a reference to the component; throws if it is missing.</summary>
    public ref T Get<T>(Entity entity)
    {
        ThrowIfDead(entity);
        if (!TryGetPool<T>(out var pool) || !pool.Has(entity.Id))
            throw new InvalidOperationException($"{entity} has no component {typeof(T).Name}.");
        return ref pool.Get(entity.Id);
    }

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

    public bool Remove<T>(Entity entity)
    {
        ThrowIfIterating();
        ThrowIfDead(entity);
        return TryGetPool<T>(out var pool) && pool.Remove(entity.Id);
    }

    // ---- queries ----

    public void Query<T1>(EntityAction<T1> action)
    {
        if (!TryGetPool<T1>(out var p1)) return;

        _iterating++;
        try
        {
            for (int i = 0; i < p1.Count; i++)
                action(Handle(p1.Dense[i]), ref p1.Components[i]);
        }
        finally { _iterating--; }
    }

    public void Query<T1, T2>(EntityAction<T1, T2> action)
    {
        if (!TryGetPool<T1>(out var p1) || !TryGetPool<T2>(out var p2)) return;

        _iterating++;
        try
        {
            // Drive from the smaller pool so we test the fewest candidates.
            if (p1.Count <= p2.Count)
            {
                for (int i = 0; i < p1.Count; i++)
                {
                    int id = p1.Dense[i];
                    if (p2.Has(id)) action(Handle(id), ref p1.Components[i], ref p2.Get(id));
                }
            }
            else
            {
                for (int i = 0; i < p2.Count; i++)
                {
                    int id = p2.Dense[i];
                    if (p1.Has(id)) action(Handle(id), ref p1.Get(id), ref p2.Components[i]);
                }
            }
        }
        finally { _iterating--; }
    }

    public void Query<T1, T2, T3>(EntityAction<T1, T2, T3> action)
    {
        if (!TryGetPool<T1>(out var p1) || !TryGetPool<T2>(out var p2) || !TryGetPool<T3>(out var p3)) return;

        _iterating++;
        try
        {
            // Simple and correct: drive from T1 and probe the others.
            for (int i = 0; i < p1.Count; i++)
            {
                int id = p1.Dense[i];
                if (p2.Has(id) && p3.Has(id))
                    action(Handle(id), ref p1.Components[i], ref p2.Get(id), ref p3.Get(id));
            }
        }
        finally { _iterating--; }
    }

    // ---- internals ----

    private Entity Handle(int id) => new(id, _generations[id]);

    private bool TryGetPool<T>(out ComponentPool<T> pool)
    {
        if (_poolsByType.TryGetValue(typeof(T), out var raw))
        {
            pool = (ComponentPool<T>)raw;
            return true;
        }

        pool = null!;
        return false;
    }

    private ComponentPool<T> GetPool<T>() => (ComponentPool<T>)_poolsByType[typeof(T)];

    private ComponentPool<T> GetOrCreatePool<T>()
    {
        if (TryGetPool<T>(out var pool)) return pool;

        pool = new ComponentPool<T>();
        _poolsByType[typeof(T)] = pool;
        _pools.Add(pool);
        return pool;
    }

    private void ThrowIfDead(Entity entity)
    {
        if (!IsAlive(entity)) throw new InvalidOperationException($"{entity} is not alive.");
    }

    private void ThrowIfIterating()
    {
        if (_iterating > 0)
            throw new InvalidOperationException(
                "Structural changes are not allowed during a query. Use a CommandBuffer and play it back afterwards.");
    }
}
