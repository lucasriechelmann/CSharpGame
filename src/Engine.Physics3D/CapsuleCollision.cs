using System.Numerics;

namespace Engine.Physics3D;

/// <summary>
/// The geometry of capsule-against-box collision: how far a capsule (a segment with a radius) overlaps an axis-aligned box and which way to
/// push it out. Pure functions, no world, so each case is unit-tested on its own.
/// </summary>
public static class CapsuleCollision
{
    /// <summary>Iterations of the closest-point search; the error shrinks by two thirds each time, so this is accurate to a millionth of the segment.</summary>
    private const int SearchIterations = 32;

    /// <summary>A distance below this counts as the segment touching or inside the box, where the push-out direction comes from the box faces instead.</summary>
    private const float InsideDistance = 1e-5f;

    /// <summary>
    /// Whether a capsule overlaps a box, and the shortest push that separates them.
    /// </summary>
    /// <param name="bottom">The centre of the lower cap.</param>
    /// <param name="top">The centre of the upper cap.</param>
    /// <param name="radius">The capsule radius.</param>
    /// <param name="box">The box.</param>
    /// <param name="normal">The unit direction, pointing from the box toward the capsule, to move the capsule along to separate them.</param>
    /// <param name="depth">How far to move the capsule along <paramref name="normal"/>.</param>
    /// <returns>True when the capsule penetrates the box (touching, with no overlap, does not count).</returns>
    public static bool TryPenetration(Vector3 bottom, Vector3 top, float radius, in Aabb3 box, out Vector3 normal, out float depth)
    {
        float distance = ClosestPoints(bottom, top, box, out var onSegment, out var onBox);
        if (distance >= radius)
        {
            normal = default;
            depth = 0f;
            return false;
        }

        if (distance > InsideDistance)
        {
            normal = (onSegment - onBox) / distance;
            depth = radius - distance;
            return true;
        }

        // The axis passes through the box: the centre is inside it, so leave by whichever face is closest, counting the capsule's own extent.
        var centre = (bottom + top) * 0.5f;
        var extent = new Vector3(radius, MathF.Abs(top.Y - bottom.Y) * 0.5f + radius, radius);
        (normal, depth) = ShortestExit(centre, extent, box);
        return true;
    }

    /// <summary>
    /// The smallest distance between a segment and a box, and the two closest points. The distance from a point on the segment to a box
    /// is a convex function of how far along the segment the point is, so a ternary search finds the minimum (an upright segment, the
    /// usual capsule, has a closed form and skips the search).
    /// </summary>
    /// <param name="a">One end of the segment.</param>
    /// <param name="b">The other end.</param>
    /// <param name="box">The box.</param>
    /// <param name="onSegment">The closest point on the segment.</param>
    /// <param name="onBox">The closest point on the box (the same point when the segment touches it).</param>
    /// <returns>The distance, zero when they touch or overlap.</returns>
    public static float ClosestPoints(Vector3 a, Vector3 b, in Aabb3 box, out Vector3 onSegment, out Vector3 onBox)
    {
        if (a.X == b.X && a.Z == b.Z)
        {
            // The usual case, an upright capsule: horizontally the distance is the same all along the segment, so only the height is a
            // question, and the closest height is any shared by the segment and the box, or the nearest end when they do not overlap.
            float segmentLow = MathF.Min(a.Y, b.Y);
            float segmentHigh = MathF.Max(a.Y, b.Y);
            float y;
            if (segmentHigh < box.Min.Y) y = segmentHigh;
            else if (segmentLow > box.Max.Y) y = segmentLow;
            else y = (MathF.Max(segmentLow, box.Min.Y) + MathF.Min(segmentHigh, box.Max.Y)) * 0.5f; // the middle of the shared range

            onSegment = new Vector3(a.X, y, a.Z);
            onBox = Vector3.Clamp(onSegment, box.Min, box.Max);
            return Vector3.Distance(onSegment, onBox);
        }

        float low = 0f;
        float high = 1f;
        if (Vector3.DistanceSquared(a, b) > 1e-12f)
        {
            for (int i = 0; i < SearchIterations; i++)
            {
                float third = (high - low) / 3f;
                float t1 = low + third;
                float t2 = high - third;
                if (DistanceSquaredToBox(Vector3.Lerp(a, b, t1), box) <= DistanceSquaredToBox(Vector3.Lerp(a, b, t2), box)) high = t2;
                else low = t1;
            }
        }

        onSegment = Vector3.Lerp(a, b, (low + high) * 0.5f);
        onBox = Vector3.Clamp(onSegment, box.Min, box.Max);
        return Vector3.Distance(onSegment, onBox);
    }

    /// <summary>The unit direction and distance to move a box of the given half-extents, centred on <paramref name="centre"/>, to just clear <paramref name="box"/> by the nearest face.</summary>
    private static (Vector3 Normal, float Depth) ShortestExit(Vector3 centre, Vector3 extent, in Aabb3 box)
    {
        var best = (Normal: Vector3.UnitY, Depth: float.MaxValue);
        void Consider(Vector3 direction, float depth)
        {
            if (depth < best.Depth) best = (direction, depth);
        }

        Consider(Vector3.UnitX, box.Max.X - centre.X + extent.X);
        Consider(-Vector3.UnitX, centre.X - box.Min.X + extent.X);
        Consider(Vector3.UnitY, box.Max.Y - centre.Y + extent.Y);
        Consider(-Vector3.UnitY, centre.Y - box.Min.Y + extent.Y);
        Consider(Vector3.UnitZ, box.Max.Z - centre.Z + extent.Z);
        Consider(-Vector3.UnitZ, centre.Z - box.Min.Z + extent.Z);
        return best;
    }

    private static float DistanceSquaredToBox(Vector3 point, in Aabb3 box) =>
        Vector3.DistanceSquared(point, Vector3.Clamp(point, box.Min, box.Max));
}
