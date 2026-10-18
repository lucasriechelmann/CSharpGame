using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Editor.Gizmos;

/// <summary>An entity's transform when a gizmo drag began: the stored (local) one, where it really is in the world, and the space it lives in.</summary>
/// <param name="Local">The entity's own <see cref="Transform"/> (relative to its parent when it has one).</param>
/// <param name="World">The same placement in world space (equal to <paramref name="Local"/> for a root).</param>
/// <param name="HasParent">Whether the entity has a parent, i.e. whether <paramref name="ParentWorld"/> matters.</param>
/// <param name="ParentWorld">The parent's world matrix (identity for a root).</param>
public readonly record struct GizmoStart(Transform Local, Transform World, bool HasParent, Matrix4x4 ParentWorld)
{
    /// <summary>Reads the start state of an entity that has a <see cref="Transform"/>.</summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="entity">The entity to capture.</param>
    public static GizmoStart Capture(World world, Entity entity)
    {
        var parent = TransformHierarchy.GetParent(world, entity);
        return new GizmoStart(
            world.Get<Transform>(entity),
            TransformHierarchy.GetWorldTransform(world, entity),
            !parent.IsNull,
            parent.IsNull ? Matrix4x4.Identity : TransformHierarchy.GetWorldMatrix(world, parent));
    }
}
