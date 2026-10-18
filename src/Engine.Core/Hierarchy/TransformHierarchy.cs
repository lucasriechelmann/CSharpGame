using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Core.Hierarchy;

/// <summary>
/// Maths over <see cref="Parent"/> links: the world matrix of an entity is its local matrix times its parent's world matrix, all the
/// way up. Pure functions over a <see cref="World"/> (no caching), so there is no cached world transform to go stale when a parent
/// moves; the walk costs one component lookup per ancestor, and an entity without a <see cref="Parent"/> costs a single lookup.
/// </summary>
/// <remarks>
/// Row-vector convention like the rest of the engine: <c>world = local * parentWorld</c>. A parent that is dead or null ends the walk,
/// so a child whose parent was destroyed behaves as a root with its local transform.
/// </remarks>
public static partial class TransformHierarchy
{
    /// <summary>The deepest chain that is followed; a longer one (or a loop that slipped in through raw component writes) throws.</summary>
    public const int MaxDepth = 64;

    /// <summary>The live parent of <paramref name="entity"/>, or <see cref="Entity.Null"/> when it has none (or it died).</summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="entity">Any entity.</param>
    public static Entity GetParent(World world, Entity entity) =>
        world.TryGet<Parent>(entity, out var parent) && world.IsAlive(parent.Value) ? parent.Value : Entity.Null;

    /// <summary>True when <paramref name="entity"/> has a live parent.</summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="entity">Any entity.</param>
    public static bool HasParent(World world, Entity entity) => !GetParent(world, entity).IsNull;

    /// <summary>The local-to-world matrix: the entity's own matrix, then each ancestor's.</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="entity">A live entity. One without a <see cref="Transform"/> counts as identity.</param>
    /// <exception cref="InvalidOperationException">The chain is longer than <see cref="MaxDepth"/>, which means it loops.</exception>
    public static Matrix4x4 GetWorldMatrix(World world, Entity entity) => GetMatrix(world, entity, null);

    /// <summary>The world-space <see cref="Transform"/> (position, rotation, scale) of an entity.</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="entity">A live entity with a <see cref="Transform"/>.</param>
    public static Transform GetWorldTransform(World world, Entity entity)
    {
        var local = world.Get<Transform>(entity);
        return HasParent(world, entity) ? Transform.FromMatrix(GetWorldMatrix(world, entity)) : local;
    }

    /// <summary>The world-space position of an entity; for a root that is <paramref name="transform"/>'s own position with no matrix maths.</summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="entity">A live entity.</param>
    /// <param name="transform">The entity's own <see cref="Transform"/> (already fetched by the query).</param>
    public static Vector3 GetWorldPosition(World world, Entity entity, in Transform transform) =>
        HasParent(world, entity) ? GetWorldMatrix(world, entity).Translation : transform.Position;

    /// <summary>
    /// The matrix to draw an entity with: like <see cref="GetWorldMatrix"/>, but every level on the way up that has a
    /// <see cref="PreviousTransform"/> is blended by <paramref name="clock"/>, so a child follows a moving parent smoothly.
    /// </summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="entity">A live entity with a <see cref="Transform"/>.</param>
    /// <param name="clock">Interpolation factor; null draws the raw simulated state.</param>
    public static Matrix4x4 GetRenderMatrix(World world, Entity entity, RenderClock? clock) => GetMatrix(world, entity, clock);

    /// <summary>
    /// The transform to draw an entity with, in world space: the interpolated own transform when it is a root (the common case, no
    /// matrix maths), otherwise the interpolated chain decomposed. Sprites and lights read position, Z rotation and scale from it.
    /// </summary>
    /// <param name="world">The world holding the entities.</param>
    /// <param name="entity">A live entity.</param>
    /// <param name="simulated">The entity's own <see cref="Transform"/> (already fetched by the query).</param>
    /// <param name="clock">Interpolation factor; null draws the raw simulated state.</param>
    public static Transform GetRenderTransform(World world, Entity entity, in Transform simulated, RenderClock? clock)
    {
        if (HasParent(world, entity)) return Transform.FromMatrix(GetMatrix(world, entity, clock));

        if (clock != null && world.TryGet<PreviousTransform>(entity, out var previous))
            return PreviousTransform.Interpolate(simulated, previous, clock.Alpha);
        return simulated;
    }

    /// <summary>
    /// Converts a world-space transform into the local transform that would produce it under <paramref name="parent"/>. With a null
    /// parent the transform is returned unchanged.
    /// </summary>
    /// <param name="world">The world holding the parent.</param>
    /// <param name="parent">The prospective parent, or <see cref="Entity.Null"/>.</param>
    /// <param name="worldTransform">The placement to keep.</param>
    /// <exception cref="InvalidOperationException">The parent's world matrix cannot be inverted (a zero scale).</exception>
    public static Transform ToLocal(World world, Entity parent, in Transform worldTransform)
    {
        if (parent.IsNull) return worldTransform;

        if (!Matrix4x4.Invert(GetWorldMatrix(world, parent), out var inverse))
            throw new InvalidOperationException($"{parent} cannot be a parent: its world matrix is not invertible (a zero scale?).");
        return Transform.FromMatrix(worldTransform.ToMatrix() * inverse);
    }

    private static Matrix4x4 GetMatrix(World world, Entity entity, RenderClock? clock)
    {
        var result = Matrix4x4.Identity;
        var current = entity;
        for (int depth = 0; !current.IsNull; depth++)
        {
            if (depth > MaxDepth)
                throw new InvalidOperationException($"{entity} has more than {MaxDepth} ancestors; the parent chain probably loops.");

            if (world.TryGet<Transform>(current, out var local))
            {
                if (clock != null && world.TryGet<PreviousTransform>(current, out var previous))
                    local = PreviousTransform.Interpolate(local, previous, clock.Alpha);
                result *= local.ToMatrix();
            }

            current = GetParent(world, current);
        }

        return result;
    }
}
