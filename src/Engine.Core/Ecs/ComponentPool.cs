namespace Engine.Core.Ecs;

internal interface IComponentPool
{
    int Count { get; }
    bool Has(int entityId);
    bool Remove(int entityId);
}

/// <summary>
/// Sparse-set storage for one component type. <c>_sparse[entityId]</c> holds the dense
/// index + 1 (0 means "absent"); <c>Dense</c> and <c>Components</c> are packed in parallel.
/// </summary>
internal sealed class ComponentPool<T> : IComponentPool
{
    private int[] _sparse = new int[64];

    internal int[] Dense = new int[16];
    internal T[] Components = new T[16];

    public int Count { get; private set; }

    public bool Has(int entityId) => (uint)entityId < (uint)_sparse.Length && _sparse[entityId] != 0;

    public ref T Get(int entityId) => ref Components[_sparse[entityId] - 1];

    /// <summary>Adds a default-initialised component. The caller guarantees it is not already present.</summary>
    public ref T Add(int entityId)
    {
        if (entityId >= _sparse.Length)
            Array.Resize(ref _sparse, Math.Max(_sparse.Length * 2, entityId + 1));
        if (Count == Dense.Length)
        {
            Array.Resize(ref Dense, Count * 2);
            Array.Resize(ref Components, Count * 2);
        }

        Dense[Count] = entityId;
        Components[Count] = default!;
        Count++;
        _sparse[entityId] = Count;
        return ref Components[Count - 1];
    }

    /// <summary>Swap-removes the component, keeping the dense arrays gap-free.</summary>
    public bool Remove(int entityId)
    {
        if (!Has(entityId)) return false;

        int index = _sparse[entityId] - 1;
        int last = Count - 1;
        if (index != last)
        {
            int movedEntity = Dense[last];
            Dense[index] = movedEntity;
            Components[index] = Components[last];
            _sparse[movedEntity] = index + 1;
        }

        Components[last] = default!;
        Count--;
        _sparse[entityId] = 0;
        return true;
    }
}
