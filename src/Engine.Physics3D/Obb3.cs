using System.Numerics;

namespace Engine.Physics3D;

/// <summary>
/// An oriented bounding box: a box that has been turned. Described by its centre, half-extents along its own axes, and the rotation that
/// turns the unit axes into them. Used for solids that are not axis-aligned (a ramp, a tilted platform), which swept axis-aligned boxes
/// cannot represent; collisions against it use the separating-axis test (<see cref="Sat"/>) and, for capsules, the box's own local space.
/// </summary>
/// <param name="Center">The centre in world space.</param>
/// <param name="HalfExtents">Half the width, height and depth along the box's own X, Y and Z axes.</param>
/// <param name="Rotation">Turns the world axes into the box's axes (must be normalised).</param>
public readonly record struct Obb3(Vector3 Center, Vector3 HalfExtents, Quaternion Rotation)
{
    /// <summary>An axis-aligned box as an oriented one with no rotation.</summary>
    /// <param name="box">The box.</param>
    public static Obb3 FromAabb(in Aabb3 box) => new(box.Center, box.Size * 0.5f, Quaternion.Identity);

    /// <summary>The box's own unit axis <paramref name="index"/> (0 = X, 1 = Y, 2 = Z) in world space.</summary>
    /// <param name="index">The axis index.</param>
    public Vector3 Axis(int index) => Vector3.Transform(index switch
    {
        0 => Vector3.UnitX,
        1 => Vector3.UnitY,
        2 => Vector3.UnitZ,
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "An axis index is 0, 1 or 2."),
    }, Rotation);

    /// <summary>The box as it sits in its own space: centred on the origin and axis-aligned. Tests against it are simple boxes tests.</summary>
    public Aabb3 LocalBox => new(-HalfExtents, HalfExtents);

    /// <summary>The smallest axis-aligned box around this one (what a world-axis query sees).</summary>
    public Aabb3 Bounds
    {
        get
        {
            var extent = Vector3.Abs(Axis(0)) * HalfExtents.X + Vector3.Abs(Axis(1)) * HalfExtents.Y + Vector3.Abs(Axis(2)) * HalfExtents.Z;
            return new Aabb3(Center - extent, Center + extent);
        }
    }

    /// <summary>A world point expressed in the box's own space (where the box is <see cref="LocalBox"/>).</summary>
    /// <param name="world">The point.</param>
    public Vector3 ToLocal(Vector3 world) => Vector3.Transform(world - Center, Quaternion.Inverse(Rotation));

    /// <summary>A direction in the box's own space turned into world space.</summary>
    /// <param name="local">The direction.</param>
    public Vector3 ToWorldDirection(Vector3 local) => Vector3.Transform(local, Rotation);

    /// <summary>The same box grown by <paramref name="margin"/> on every side.</summary>
    /// <param name="margin">How much to add to each half-extent.</param>
    public Obb3 Inflated(float margin) => this with { HalfExtents = HalfExtents + new Vector3(margin) };
}
