using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

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
    public static Entity Duplicate(World world, Entity source, Vector2 offset)
    {
        var types = new List<Type>();
        world.GetComponentTypes(source, types); // throws for a dead entity, naming it

        var copy = world.CreateEntity();
        foreach (var type in types) world.AddBoxed(copy, type, world.GetBoxed(source, type));

        if (world.TryGet<Transform>(copy, out _))
            world.Get<Transform>(copy).Position += new Vector3(offset, 0f);
        if (world.TryGet<Name>(copy, out var name))
            world.Get<Name>(copy) = Name.Of(name.Value + " (copy)");
        if (world.Has<PreviousTransform>(copy))
            world.Get<PreviousTransform>(copy) = default; // draw the copy where it is, not swept from the original's history

        return copy;
    }
}
