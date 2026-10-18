namespace Engine.Core.Ecs;

// Queries: visit every entity that has all of the requested components.
// While a query runs, structural changes throw; queue them in a CommandBuffer instead.
public sealed partial class World
{
    /// <summary>Visits every entity that has component <typeparamref name="T1"/>.</summary>
    public void Query<T1>(EntityAction<T1> action)
    {
        if (!TryGetPool<T1>(out var p1)) return;

        _iterating++;
        try
        {
            for (int i = 0; i < p1.Count; i++)
                action(_allocator.HandleFor(p1.Dense[i]), ref p1.Components[i]);
        }
        finally { _iterating--; }
    }

    /// <summary>Visits every entity that has both <typeparamref name="T1"/> and <typeparamref name="T2"/>.</summary>
    public void Query<T1, T2>(EntityAction<T1, T2> action)
    {
        if (!TryGetPool<T1>(out var p1) || !TryGetPool<T2>(out var p2)) return;

        _iterating++;
        try
        {
            // Drive from the smaller pool so we probe the fewest candidates.
            if (p1.Count <= p2.Count)
            {
                for (int i = 0; i < p1.Count; i++)
                {
                    int id = p1.Dense[i];
                    if (p2.Has(id)) action(_allocator.HandleFor(id), ref p1.Components[i], ref p2.Get(id));
                }
            }
            else
            {
                for (int i = 0; i < p2.Count; i++)
                {
                    int id = p2.Dense[i];
                    if (p1.Has(id)) action(_allocator.HandleFor(id), ref p1.Get(id), ref p2.Components[i]);
                }
            }
        }
        finally { _iterating--; }
    }

    /// <summary>Visits every entity that has <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/>.</summary>
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
                    action(_allocator.HandleFor(id), ref p1.Components[i], ref p2.Get(id), ref p3.Get(id));
            }
        }
        finally { _iterating--; }
    }
}
