using System.Numerics;
using Engine.Core.Components;

namespace Engine.Runtime.Editor.Gizmos;

/// <summary>
/// Applies a gizmo drag (a world-space <see cref="GizmoDelta"/>) to an entity and returns the <see cref="Transform"/> to store. Always
/// computed from the transform the drag started with, never from the previous frame's result, so there is no drift over a long drag.
/// Children store local transforms, so the world-space result is converted back through the parent.
/// </summary>
public static class GizmoApply
{
    /// <summary>The new stored transform for one entity.</summary>
    /// <param name="mode">What the drag did.</param>
    /// <param name="start">The entity's transform when the drag began.</param>
    /// <param name="delta">The drag's total change.</param>
    /// <param name="pivot">The gizmo's origin; a rotation turns positions around it (for one entity that is its own position, so only its orientation changes).</param>
    /// <exception cref="InvalidOperationException">The entity has a parent whose world matrix cannot be inverted (a zero scale).</exception>
    public static Transform Apply(GizmoMode mode, in GizmoStart start, in GizmoDelta delta, Vector3 pivot)
    {
        var result = start.Local;
        switch (mode)
        {
            case GizmoMode.Translate:
                result.Position = ToLocalPosition(start, start.World.Position + delta.Translation);
                break;

            case GizmoMode.Rotate:
                var position = pivot + Vector3.Transform(start.World.Position - pivot, delta.Rotation);
                var rotation = Quaternion.Normalize(delta.Rotation * start.World.Rotation); // the turn comes after the current orientation: a spin about a world axis
                result.Position = ToLocalPosition(start, position);
                result.Rotation = start.HasParent ? Quaternion.Normalize(Quaternion.Inverse(ParentRotation(start)) * rotation) : rotation;
                break;

            default:
                result.Scale = start.Local.Scale * delta.Scale;
                break;
        }

        return result;
    }

    private static Vector3 ToLocalPosition(in GizmoStart start, Vector3 worldPosition)
    {
        if (!start.HasParent) return worldPosition;

        if (!Matrix4x4.Invert(start.ParentWorld, out var inverse))
            throw new InvalidOperationException("The parent's world matrix is not invertible (does it have a zero scale?).");
        return Vector3.Transform(worldPosition, inverse);
    }

    private static Quaternion ParentRotation(in GizmoStart start) =>
        Matrix4x4.Decompose(start.ParentWorld, out _, out var rotation, out _) ? rotation : Quaternion.Identity;
}
