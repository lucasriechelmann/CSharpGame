using Engine.Core.Ecs;

namespace Engine.Runtime.Editor.Undo;

/// <summary>
/// Gives entities a stable number that survives being destroyed and recreated by undo. Entity handles carry a generation that
/// goes stale on destroy, so a command that stored one could never find the entity again after "delete, undo delete". The commands
/// store the tracked number instead, and the tracker is re-pointed at the new handle when the entity is recreated.
/// </summary>
public sealed class EntityTracker
{
    private readonly Dictionary<Entity, int> _idsByEntity = new();
    private readonly Dictionary<int, Entity> _entitiesById = new();
    private int _next = 1;

    /// <summary>The tracked number of <paramref name="entity"/>, assigned the first time it is asked for.</summary>
    /// <param name="entity">A live entity.</param>
    public int IdOf(Entity entity)
    {
        if (entity.IsNull) throw new ArgumentException("The null entity cannot be tracked.", nameof(entity));
        if (_idsByEntity.TryGetValue(entity, out int id)) return id;

        id = _next++;
        _idsByEntity[entity] = id;
        _entitiesById[id] = entity;
        return id;
    }

    /// <summary>The entity currently bound to <paramref name="id"/>, or <see cref="Entity.Null"/> when it was never tracked or is unbound (destroyed by an undo or redo).</summary>
    /// <param name="id">A number from <see cref="IdOf"/>.</param>
    public Entity Lookup(int id) => _entitiesById.TryGetValue(id, out var entity) ? entity : Entity.Null;

    /// <summary>Points <paramref name="id"/> at <paramref name="entity"/>, the freshly recreated entity.</summary>
    /// <param name="id">The tracked number to keep.</param>
    /// <param name="entity">The new handle.</param>
    public void Bind(int id, Entity entity)
    {
        Unbind(id);
        _idsByEntity[entity] = id;
        _entitiesById[id] = entity;
    }

    /// <summary>Forgets the entity behind <paramref name="id"/> (it was destroyed); the number stays reserved so a later <see cref="Bind"/> reuses it.</summary>
    /// <param name="id">The tracked number.</param>
    public void Unbind(int id)
    {
        if (!_entitiesById.TryGetValue(id, out var entity)) return;

        _idsByEntity.Remove(entity);
        _entitiesById[id] = Entity.Null;
    }

    /// <summary>Forgets everything. Call when the world is replaced, which makes every handle stale.</summary>
    public void Clear()
    {
        _idsByEntity.Clear();
        _entitiesById.Clear();
    }
}
