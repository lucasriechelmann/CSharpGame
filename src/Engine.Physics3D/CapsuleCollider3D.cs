using System.Numerics;

namespace Engine.Physics3D;

/// <summary>
/// An upright capsule collider (a cylinder with a hemisphere on each end): the natural shape for a character, because it slides
/// round corners and over small bumps instead of catching on them as a box would. A sphere is a capsule as tall as it is wide
/// (<see cref="Sphere"/>). It follows the entity's <see cref="Engine.Core.Components.Transform"/> position and always stands upright:
/// rotation and scale are ignored, so size comes from <see cref="Radius"/> and <see cref="Height"/> alone.
/// </summary>
/// <remarks>
/// A body with this collider is moved by <see cref="PhysicsSystem3D"/> like a box body (gravity, friction, momentum with other bodies),
/// but collides by pushing out of whatever it overlaps rather than by sweeping a box, so it can slide along walls and slopes of boxes.
/// Other bodies see it as its bounding box.
/// </remarks>
public struct CapsuleCollider3D
{
    /// <summary>Radius of the cylinder and of both end caps, in world units.</summary>
    public float Radius;

    /// <summary>Total height from the bottom of the lower cap to the top of the upper cap. At least twice the radius (a sphere); smaller values count as that.</summary>
    public float Height;

    /// <summary>Offset of the capsule's centre from the entity's position (for example to stand the capsule on its pivot).</summary>
    public Vector3 Offset;

    /// <summary>Creates a capsule centred on the entity.</summary>
    /// <param name="radius">Radius in world units.</param>
    /// <param name="height">Total height in world units.</param>
    public static CapsuleCollider3D Create(float radius, float height) => new() { Radius = radius, Height = height };

    /// <summary>Creates a sphere collider: a capsule exactly as tall as it is wide.</summary>
    /// <param name="radius">Radius in world units.</param>
    public static CapsuleCollider3D Sphere(float radius) => new() { Radius = radius, Height = radius * 2f };

    /// <summary>Half the length of the straight part between the two cap centres (zero for a sphere).</summary>
    public readonly float HalfSegment => MathF.Max(Height * 0.5f - Radius, 0f);

    /// <summary>The two cap centres in world space for an entity at <paramref name="position"/>: the axis of the capsule, bottom first.</summary>
    /// <param name="position">The entity's position.</param>
    public readonly (Vector3 Bottom, Vector3 Top) GetSegment(Vector3 position)
    {
        var centre = position + Offset;
        float half = HalfSegment;
        return (centre - new Vector3(0f, half, 0f), centre + new Vector3(0f, half, 0f));
    }

    /// <summary>The smallest axis-aligned box around the capsule, which is how other bodies see it.</summary>
    /// <param name="position">The entity's position.</param>
    public readonly Aabb3 GetBounds(Vector3 position)
    {
        var centre = position + Offset;
        var extent = new Vector3(Radius, HalfSegment + Radius, Radius);
        return new Aabb3(centre - extent, centre + extent);
    }
}
