using System.Numerics;

namespace Engine.Physics2D;

/// <summary>An axis-aligned bounding box in world space (Y up).</summary>
/// <param name="Min">Bottom-left corner.</param>
/// <param name="Max">Top-right corner.</param>
public readonly record struct Aabb(Vector2 Min, Vector2 Max)
{
    /// <summary>Builds a box from its centre and full size.</summary>
    public static Aabb FromCenter(Vector2 center, Vector2 size)
    {
        var half = size * 0.5f;
        return new Aabb(center - half, center + half);
    }

    /// <summary>The centre point.</summary>
    public Vector2 Center => (Min + Max) * 0.5f;

    /// <summary>Width and height.</summary>
    public Vector2 Size => Max - Min;

    /// <summary>
    /// True when the interiors overlap. Boxes that merely share an edge or corner do not overlap, which is what
    /// lets a body rest on a floor or slide along a wall.
    /// </summary>
    public bool Overlaps(in Aabb other) =>
        Min.X < other.Max.X && Max.X > other.Min.X &&
        Min.Y < other.Max.Y && Max.Y > other.Min.Y;

    /// <summary>The same box moved by <paramref name="offset"/>.</summary>
    public Aabb Translated(Vector2 offset) => new(Min + offset, Max + offset);

    /// <summary>The lower edge coordinate along an axis.</summary>
    public float GetMin(Axis axis) => axis == Axis.X ? Min.X : Min.Y;

    /// <summary>The upper edge coordinate along an axis.</summary>
    public float GetMax(Axis axis) => axis == Axis.X ? Max.X : Max.Y;
}
