using System.Numerics;
using Engine.Core.Components;

namespace Engine.Physics3D;

/// <summary>
/// An axis-aligned box collider attached to an entity. It follows the entity's <see cref="Transform"/> position and never rotates or
/// scales: collision size comes from <see cref="Size"/> alone, so a rotating mesh keeps a fixed box (rotated shapes need the later
/// rigid-body work).
/// </summary>
public struct BoxCollider3D
{
    /// <summary>Full width, height and depth of the box in world units.</summary>
    public Vector3 Size;

    /// <summary>Offset of the box centre from the entity's position (for example to stand a box on its pivot).</summary>
    public Vector3 Offset;

    /// <summary>Creates a collider centred on the entity.</summary>
    /// <param name="size">Full width, height and depth in world units.</param>
    public static BoxCollider3D Create(Vector3 size) => new() { Size = size };

    /// <summary>The collider's world-space box for an entity at <paramref name="position"/>.</summary>
    public readonly Aabb3 GetBounds(Vector3 position) => Aabb3.FromCenter(position + Offset, Size);
}
