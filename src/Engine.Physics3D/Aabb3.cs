using System.Numerics;

namespace Engine.Physics3D;

/// <summary>An axis-aligned bounding box in world space (Y up).</summary>
/// <param name="Min">Corner with the smallest coordinates.</param>
/// <param name="Max">Corner with the largest coordinates.</param>
public readonly record struct Aabb3(Vector3 Min, Vector3 Max)
{
    /// <summary>Builds a box from its centre and full size.</summary>
    public static Aabb3 FromCenter(Vector3 center, Vector3 size)
    {
        var half = size * 0.5f;
        return new Aabb3(center - half, center + half);
    }

    /// <summary>The centre point.</summary>
    public Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>Width, height and depth.</summary>
    public Vector3 Size => Max - Min;

    /// <summary>
    /// True when the interiors overlap. Boxes that merely share a face, edge or corner do not overlap, which is what lets a body rest
    /// on a floor or slide along a wall.
    /// </summary>
    public bool Overlaps(in Aabb3 other) =>
        Min.X < other.Max.X && Max.X > other.Min.X &&
        Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
        Min.Z < other.Max.Z && Max.Z > other.Min.Z;

    /// <summary>The same box moved by <paramref name="offset"/>.</summary>
    public Aabb3 Translated(Vector3 offset) => new(Min + offset, Max + offset);

    /// <summary>The lower face coordinate along an axis.</summary>
    public float GetMin(Axis3D axis) => AxisMath.Get(Min, axis);

    /// <summary>The upper face coordinate along an axis.</summary>
    public float GetMax(Axis3D axis) => AxisMath.Get(Max, axis);
}
