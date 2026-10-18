namespace Engine.Core.Ecs;

/// <summary>
/// Hands out entity ids and tracks their generations. It knows nothing about components;
/// <see cref="World"/> combines it with component storage.
/// </summary>
internal sealed class EntityAllocator
{
    // Slot 0 is unused so Entity.Null (id 0, generation 0) can never alias a real entity.
    private readonly List<int> _generations = new() { 0 };

    // Parallel to _generations: whether the slot currently holds a live entity (needed to enumerate them).
    private readonly List<bool> _alive = new() { false };

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
            _alive.Add(false);
        }

        _alive[id] = true;
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
        Release(entity.Id);
        AliveCount--;
    }

    /// <summary>Destroys every live entity at once; all outstanding handles go stale.</summary>
    public void DestroyAll()
    {
        for (int id = 1; id < _alive.Count; id++)
            if (_alive[id]) Release(id);

        AliveCount = 0;
    }

    /// <summary>Builds the current handle for a live slot id (used when iterating packed component arrays).</summary>
    public Entity HandleFor(int id) => new(id, _generations[id]);

    /// <summary>Yields every live entity in ascending id order.</summary>
    public IEnumerable<Entity> Enumerate()
    {
        for (int id = 1; id < _alive.Count; id++)
            if (_alive[id]) yield return new Entity(id, _generations[id]);
    }

    private void Release(int id)
    {
        _generations[id]++;
        _alive[id] = false;
        _freeIds.Push(id);
    }
}
