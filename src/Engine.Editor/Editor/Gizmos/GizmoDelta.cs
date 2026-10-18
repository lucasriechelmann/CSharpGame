using System.Numerics;

namespace Engine.Editor.Gizmos;

/// <summary>
/// How far a drag has moved the gizmo since it started, as a world-space change rather than a new transform, so the same drag can be
/// applied to one entity or to a whole selection (see <see cref="GizmoApply"/>).
/// </summary>
/// <param name="Translation">The world-space offset to add to positions.</param>
/// <param name="Rotation">A turn about a world axis through the gizmo's origin.</param>
/// <param name="Scale">The factor for each of the entity's own axes (all equal for a uniform scale).</param>
public readonly record struct GizmoDelta(Vector3 Translation, Quaternion Rotation, Vector3 Scale)
{
    /// <summary>No change at all.</summary>
    public static GizmoDelta None => new(Vector3.Zero, Quaternion.Identity, Vector3.One);
}
