namespace Engine.Core.Ecs;

/// <summary>
/// Hands out entity ids and tracks their generations. It knows nothing about components;
/// <see cref="World"/> combines it with component storage.
/// </summary>
internal sealed class EntityAllocator
{
    // Slot 0 is unused so Entity.Null (id 0, generation 0) can never alias a real entity.
    private readonly List<int> _generations = new() { 0 };

    private readonly Stack<int> _freeIds = new();

    /// <summary>Number of entities currently alive.</summary>
    public int AliveCount { get; private set; }

    /// <summary>Creates an entity, reusing a freed slot when one is available.</summary>
    public Entity Create()
    {
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

        AliveCount++;
        return new Entity(id, _generations[id]);
    }

    /// <summary>True when the handle refers to a live entity (right slot, right generation).</summary>
    public bool IsAlive(Entity entity) =>
        entity.Generation != 0
        && (uint)entity.Id < (uint)_generations.Count
        && _generations[entity.Id] == entity.Generation;

    /// <summary>Frees the entity's slot and bumps its generation so old handles go stale. The caller checks liveness first.</summary>
    public void Destroy(Entity entity)
    {
        _generations[entity.Id]++;
        _freeIds.Push(entity.Id);
        AliveCount--;
    }

    /// <summary>Builds the current handle for a live slot id (used when iterating packed component arrays).</summary>
    public Entity HandleFor(int id) => new(id, _generations[id]);
}
