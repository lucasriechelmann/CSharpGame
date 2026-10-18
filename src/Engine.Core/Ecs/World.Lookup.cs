using Engine.Core.Components;

namespace Engine.Core.Ecs;

// Lookup helpers for setup code: find one well-known entity (the player, the camera target) or count a kind of entity without writing a query.
// They walk a pool directly, so they allocate nothing, but they are linear: look entities up once when a scene loads, not every frame.
public sealed partial class World
{
    /// <summary>The first entity whose <see cref="Name"/> equals <paramref name="name"/> (ordinal), or <see cref="Entity.Null"/> when there is none.</summary>
    public Entity FindByName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!TryGetPool<Name>(out var names)) return Entity.Null;

        for (int i = 0; i < names.Count; i++)
        {
            if (string.Equals(names.Components[i].Value, name, StringComparison.Ordinal)) return _allocator.HandleFor(names.Dense[i]);
        }

        return Entity.Null;
    }

    /// <summary>Like <see cref="FindByName"/>, but throws when nothing has that name, for entities a game cannot run without.</summary>
    public Entity RequireByName(string name)
    {
        var found = FindByName(name);
        return found.IsNull ? throw new InvalidOperationException($"The scene has no entity named '{name}'.") : found;
    }

    /// <summary>The first entity that has component <typeparamref name="T"/>, or <see cref="Entity.Null"/> when there is none.</summary>
    public Entity FindFirst<T>() =>
        TryGetPool<T>(out var pool) && pool.Count > 0 ? _allocator.HandleFor(pool.Dense[0]) : Entity.Null;

    /// <summary>Like <see cref="FindFirst{T}"/>, but throws when no entity has the component.</summary>
    public Entity RequireFirst<T>()
    {
        var found = FindFirst<T>();
        return found.IsNull ? throw new InvalidOperationException($"The scene has no entity with a {typeof(T).Name} component.") : found;
    }

    /// <summary>How many entities have component <typeparamref name="T"/>.</summary>
    public int CountWith<T>() => TryGetPool<T>(out var pool) ? pool.Count : 0;
}
