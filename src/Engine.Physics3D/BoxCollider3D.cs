using System.Numerics;
using Engine.Core.Components;

namespace Engine.Physics3D;

/// <summary>
/// A box collider attached to an entity. Normally it follows the entity's <see cref="Transform"/> position and never rotates or scales:
/// collision size comes from <see cref="Size"/> alone, so a rotating mesh keeps a fixed axis-aligned box. A static solid can opt in to
/// <see cref="Oriented"/> and then turns with the entity (a ramp, a tilted platform).
/// </summary>
public struct BoxCollider3D
{
    /// <summary>Full width, height and depth of the box in world units (before any rotation).</summary>
    public Vector3 Size;

    /// <summary>Offset of the box centre from the entity's position (for example to stand a box on its pivot); turned with the box when <see cref="Oriented"/>.</summary>
    public Vector3 Offset;

    /// <summary>
    /// For a <see cref="StaticSolid3D"/>: take the entity's rotation, so the box can be a ramp or a tilted wall. Moving bodies do not rotate,
    /// so setting this on one is an error. Scale is still ignored; <see cref="Size"/> is the size.
    /// </summary>
    public bool Oriented;

    /// <summary>Creates a collider centred on the entity.</summary>
    /// <param name="size">Full width, height and depth in world units.</param>
    public static BoxCollider3D Create(Vector3 size) => new() { Size = size };

    /// <summary>The collider's world-space box for an entity at <paramref name="position"/>, ignoring rotation.</summary>
    public readonly Aabb3 GetBounds(Vector3 position) => Aabb3.FromCenter(position + Offset, Size);

    /// <summary>The collider as an oriented box for an entity at <paramref name="position"/> turned by <paramref name="rotation"/>.</summary>
    /// <param name="position">The entity's world position.</param>
    /// <param name="rotation">The entity's world rotation.</param>
    public readonly Obb3 GetObb(Vector3 position, Quaternion rotation) =>
        new(position + Vector3.Transform(Offset, rotation), Size * 0.5f, rotation);
}
