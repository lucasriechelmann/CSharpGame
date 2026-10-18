using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Runtime.Editor;

/// <summary>Copies an entity with all of its components (the editor's Duplicate command).</summary>
public static class EntityCloner
{
    /// <summary>
    /// Creates a new entity with a copy of every component of <paramref name="source"/>. The copy is nudged by
    /// <paramref name="offset"/> so it does not hide the original, and a <see cref="Name"/> gets " (copy)" appended.
    /// </summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="source">The entity to copy; must be alive.</param>
    /// <param name="offset">Added to the copy's position.</param>
    /// <returns>The new entity.</returns>
    /// <remarks>
    /// Components are copied by value. A component that holds an <c>Entity</c> reference would point at the original's target; scenes
    /// forbid those for now, so this matches what a save and load would do.
    /// </remarks>
    public static Entity Duplicate(World world, Entity source, Vector2 offset) => Duplicate(world, source, new Vector3(offset, 0f));

    /// <summary>Like <see cref="Duplicate(World, Entity, Vector2)"/> with a 3D offset, for 3D scenes (where the floor is the XZ plane).</summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="source">The entity to copy; must be alive.</param>
    /// <param name="offset">Added to the copy's position.</param>
    /// <returns>The new entity.</returns>
    public static Entity Duplicate(World world, Entity source, Vector3 offset)
    {
        var types = new List<Type>();
        world.GetComponentTypes(source, types); // throws for a dead entity, naming it

        var copy = world.CreateEntity();
        foreach (var type in types) world.AddBoxed(copy, type, world.GetBoxed(source, type));

        if (world.TryGet<Transform>(copy, out _))
            world.Get<Transform>(copy).Position += offset;
        if (world.TryGet<Name>(copy, out var name))
            world.Get<Name>(copy) = Name.Of(name.Value + " (copy)");
        if (world.Has<PreviousTransform>(copy))
            world.Get<PreviousTransform>(copy) = default; // draw the copy where it is, not swept from the original's history

        return copy;
    }

    /// <summary>
    /// Copies <paramref name="source"/> and everything below it. The copy of the root becomes a sibling of the original (same parent) and
    /// is nudged by <paramref name="offset"/>; the copies of the descendants hang below the new root with the same local transforms and
    /// keep their names.
    /// </summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="source">The root of the subtree to copy; must be alive.</param>
    /// <param name="offset">Added to the new root's position.</param>
    /// <returns>The copies, parents before children; the first is the copy of <paramref name="source"/>.</returns>
    public static List<Entity> DuplicateSubtree(World world, Entity source, Vector2 offset) => DuplicateSubtree(world, source, new Vector3(offset, 0f));

    /// <summary>Like <see cref="DuplicateSubtree(World, Entity, Vector2)"/> with a 3D offset.</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="source">The root of the subtree to copy; must be alive.</param>
    /// <param name="offset">Added to the new root's position.</param>
    /// <returns>The copies, parents before children; the first is the copy of <paramref name="source"/>.</returns>
    public static List<Entity> DuplicateSubtree(World world, Entity source, Vector3 offset)
    {
        var originals = new List<Entity>();
        TransformHierarchy.GetSubtree(world, source, originals);

        var copies = new List<Entity>(originals.Count);
        var copyOf = new Dictionary<Entity, Entity>(originals.Count);
        for (int i = 0; i < originals.Count; i++)
        {
            var original = originals[i];
            var copy = Duplicate(world, original, i == 0 ? offset : Vector3.Zero);
            if (i > 0)
            {
                if (world.TryGet<Name>(original, out var name)) world.Get<Name>(copy) = name; // only the root is labelled "(copy)"
                world.Set(copy, new Parent { Value = copyOf[TransformHierarchy.GetParent(world, original)] });
            }

            copyOf[original] = copy;
            copies.Add(copy);
        }

        return copies;
    }
}
