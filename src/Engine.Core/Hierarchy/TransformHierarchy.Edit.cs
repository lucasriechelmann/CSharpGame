using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Core.Hierarchy;

// Changing the tree: attach, detach, list children, destroy a subtree.
public static partial class TransformHierarchy
{
    /// <summary>True when <paramref name="entity"/> is <paramref name="ancestor"/> or sits anywhere below it.</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="entity">The entity to test.</param>
    /// <param name="ancestor">The possible ancestor.</param>
    public static bool IsSelfOrDescendantOf(World world, Entity entity, Entity ancestor)
    {
        var current = entity;
        for (int depth = 0; !current.IsNull && depth <= MaxDepth; depth++)
        {
            if (current == ancestor) return true;
            current = GetParent(world, current);
        }

        return false;
    }

    /// <summary>
    /// Makes <paramref name="child"/> a child of <paramref name="parent"/>, or a root when <paramref name="parent"/> is null.
    /// </summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="child">The entity to move.</param>
    /// <param name="parent">The new parent, or <see cref="Entity.Null"/> to detach.</param>
    /// <param name="keepWorld">
    /// True rewrites the child's local transform so it stays where it is in the world (what dragging in the editor wants); false
    /// keeps the local values, so the child jumps with its new parent (what building a prefab in code wants).
    /// </param>
    /// <exception cref="InvalidOperationException">An entity is dead, or the parent is the child or one of its descendants (a loop).</exception>
    public static void SetParent(World world, Entity child, Entity parent, bool keepWorld)
    {
        if (!world.IsAlive(child)) throw new InvalidOperationException($"{child} is not alive.");
        if (!parent.IsNull && !world.IsAlive(parent)) throw new InvalidOperationException($"{parent} is not alive.");
        if (!parent.IsNull && IsSelfOrDescendantOf(world, parent, child))
            throw new InvalidOperationException($"{parent} cannot become the parent of {child}: it is that entity or one of its descendants.");

        bool rewrite = keepWorld && world.Has<Transform>(child);
        var placement = rewrite ? GetWorldTransform(world, child) : default;

        if (parent.IsNull) world.Remove<Parent>(child);
        else world.Set(child, new Parent { Value = parent });

        if (rewrite) world.Get<Transform>(child) = ToLocal(world, parent, placement);
        if (world.Has<PreviousTransform>(child)) world.Get<PreviousTransform>(child) = default; // draw where it is, not swept from the old space
    }

    /// <summary>Adds every live direct child of <paramref name="parent"/> to <paramref name="children"/>, in entity id order. Scans every <see cref="Parent"/>; meant for tools, not per-frame use.</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="parent">The parent.</param>
    /// <param name="children">Receives the children (not cleared first).</param>
    public static void GetChildren(World world, Entity parent, List<Entity> children)
    {
        int start = children.Count;
        var found = children;
        world.Query((Entity entity, ref Parent link) =>
        {
            if (link.Value == parent) found.Add(entity);
        });
        children.Sort(start, children.Count - start, EntityIdComparer.Instance);
    }

    /// <summary>Adds <paramref name="root"/> and every descendant to <paramref name="subtree"/>, parents before their children (so creating in this order and destroying in reverse is always valid).</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="root">The top of the subtree.</param>
    /// <param name="subtree">Receives the entities (not cleared first).</param>
    public static void GetSubtree(World world, Entity root, List<Entity> subtree)
    {
        int index = subtree.Count;
        subtree.Add(root);
        var children = new List<Entity>();
        while (index < subtree.Count)
        {
            children.Clear();
            GetChildren(world, subtree[index++], children);
            subtree.AddRange(children);
        }
    }

    /// <summary>Destroys <paramref name="root"/> and everything below it.</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="root">The top of the subtree.</param>
    public static void Destroy(World world, Entity root)
    {
        var subtree = new List<Entity>();
        GetSubtree(world, root, subtree);
        for (int i = subtree.Count - 1; i >= 0; i--) world.DestroyEntity(subtree[i]);
    }

    private sealed class EntityIdComparer : IComparer<Entity>
    {
        public static readonly EntityIdComparer Instance = new();

        public int Compare(Entity x, Entity y) => x.Id.CompareTo(y.Id);
    }
}
