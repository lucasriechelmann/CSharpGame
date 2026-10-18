namespace Engine.Core.Ecs;

// Pool lookup and precondition guards shared by the other World partials.
public sealed partial class World
{
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
