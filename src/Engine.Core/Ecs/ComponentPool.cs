namespace Engine.Core.Ecs;

/// <summary>
/// Sparse-set storage for one component type. <c>_sparse[entityId]</c> holds the dense
/// index + 1 (0 means "absent"); <see cref="Dense"/> and <see cref="Components"/> are packed in parallel.
/// </summary>
/// <typeparam name="T">The component type stored in this pool.</typeparam>
internal sealed class ComponentPool<T> : IComponentPool
{
    private int[] _sparse = new int[64];

    /// <summary>Entity ids, packed; <c>Dense[i]</c> owns <c>Components[i]</c>.</summary>
    internal int[] Dense = new int[16];

    /// <summary>Component values, packed in the same order as <see cref="Dense"/> for cache-friendly iteration.</summary>
    internal T[] Components = new T[16];

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public bool Has(int entityId) => (uint)entityId < (uint)_sparse.Length && _sparse[entityId] != 0;

    /// <summary>Returns a reference to the entity's component. The caller guarantees it exists.</summary>
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
