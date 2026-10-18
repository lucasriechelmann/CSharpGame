using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Runtime.Editor;

/// <summary>
/// The set of entities the editor has selected. The most recently selected one is the <see cref="Primary"/>: the inspector shows it and the
/// gizmo takes its orientation, while commands (move, rotate, scale, duplicate, delete) act on the whole set.
/// </summary>
/// <remarks>
/// Plain data over entity handles, no ImGui, so it is unit-tested. Handles can go stale when entities are destroyed (delete, undo of a
/// create, Stop), so the editor calls <see cref="Prune"/> once per frame.
/// </remarks>
public sealed class EditorSelection
{
    private readonly List<Entity> _items = new();

    /// <summary>The selected entities, oldest first; the last one is the <see cref="Primary"/>.</summary>
    public IReadOnlyList<Entity> Items => _items;

    /// <summary>How many entities are selected.</summary>
    public int Count => _items.Count;

    /// <summary>The most recently selected entity, or <see cref="Entity.Null"/> when nothing is selected.</summary>
    public Entity Primary => _items.Count > 0 ? _items[^1] : Entity.Null;

    /// <summary>True when <paramref name="entity"/> is selected.</summary>
    /// <param name="entity">Any entity.</param>
    public bool Contains(Entity entity) => _items.Contains(entity);

    /// <summary>Selects just <paramref name="entity"/>; the null entity clears the selection.</summary>
    /// <param name="entity">The entity to select.</param>
    public void SetOnly(Entity entity)
    {
        _items.Clear();
        if (!entity.IsNull) _items.Add(entity);
    }

    /// <summary>Adds <paramref name="entity"/> if it is not selected, or removes it if it is (a Ctrl+click). An added entity becomes the primary.</summary>
    /// <param name="entity">The entity to toggle; the null entity is ignored.</param>
    public void Toggle(Entity entity)
    {
        if (entity.IsNull) return;

        if (!_items.Remove(entity)) _items.Add(entity);
    }

    /// <summary>Adds <paramref name="entity"/> as the primary (moving it there if it was already selected).</summary>
    /// <param name="entity">The entity to add; the null entity is ignored.</param>
    public void Add(Entity entity)
    {
        if (entity.IsNull) return;

        _items.Remove(entity);
        _items.Add(entity);
    }

    /// <summary>Selects nothing.</summary>
    public void Clear() => _items.Clear();

    /// <summary>Forgets entities that no longer exist.</summary>
    /// <param name="world">The world the entities belong to.</param>
    public void Prune(World world) => _items.RemoveAll(entity => !world.IsAlive(entity));

    /// <summary>
    /// Copies the selected entities that have no selected ancestor into <paramref name="result"/>. Moving, copying or deleting a parent already
    /// takes its children along, so acting on both would move them twice.
    /// </summary>
    /// <param name="world">The world the entities belong to.</param>
    /// <param name="result">Receives the entities in selection order (cleared first).</param>
    public void GetTopLevel(World world, List<Entity> result)
    {
        result.Clear();
        foreach (var entity in _items)
        {
            bool covered = false;
            for (var ancestor = TransformHierarchy.GetParent(world, entity); !ancestor.IsNull && !covered; ancestor = TransformHierarchy.GetParent(world, ancestor))
                covered = _items.Contains(ancestor);

            if (!covered) result.Add(entity);
        }
    }
}
