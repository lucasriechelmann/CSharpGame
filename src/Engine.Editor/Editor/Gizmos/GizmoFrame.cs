using System.Numerics;

namespace Engine.Editor.Gizmos;

/// <summary>Where the gizmo stands and which way its axes point.</summary>
/// <param name="Origin">The world position of the gizmo (the entity, or the middle of a selection).</param>
/// <param name="Orientation">Turns the unit X, Y and Z axes into the gizmo's axes: identity for world space, the entity's rotation for local space.</param>
public readonly record struct GizmoFrame(Vector3 Origin, Quaternion Orientation)
{
    /// <summary>The unit direction of axis 0 (X), 1 (Y) or 2 (Z) in world space.</summary>
    /// <param name="axis">The axis index.</param>
    public Vector3 Axis(int axis) => Vector3.Transform(axis switch
    {
        0 => Vector3.UnitX,
        1 => Vector3.UnitY,
        2 => Vector3.UnitZ,
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "An axis index is 0, 1 or 2."),
    }, Orientation);
}
