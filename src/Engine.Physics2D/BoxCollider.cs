using System.Numerics;
using Engine.Core.Components;

namespace Engine.Physics2D;

/// <summary>An axis-aligned box collider attached to an entity. It follows the entity's <see cref="Transform"/> position and never rotates.</summary>
public struct BoxCollider
{
    /// <summary>Full width and height of the box in world units.</summary>
    public Vector2 Size;

    /// <summary>Offset of the box centre from the entity's position (for example to align with a sprite's pivot).</summary>
    public Vector2 Offset;

    /// <summary>Creates a collider centred on the entity.</summary>
    /// <param name="size">Full width and height in world units.</param>
    public static BoxCollider Create(Vector2 size) => new() { Size = size };

    /// <summary>The collider's world-space box for an entity at <paramref name="position"/>.</summary>
    public readonly Aabb GetBounds(Vector3 position) =>
        Aabb.FromCenter(new Vector2(position.X, position.Y) + Offset, Size);
}
