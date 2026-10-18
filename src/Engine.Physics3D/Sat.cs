using System.Numerics;

namespace Engine.Physics3D;

/// <summary>
/// The separating-axis test for two oriented boxes. Two convex shapes are apart exactly when some axis exists on which their shadows do
/// not overlap; for boxes it is enough to try the 15 axes made from the face directions of each (3 + 3) and the cross products of one edge
/// direction of each (9). When every axis overlaps the boxes intersect, and the axis with the least overlap is the shortest way to push them
/// apart. Pure maths, unit-tested.
/// </summary>
public static class Sat
{
    /// <summary>A cross product shorter than this means two edges are parallel, so that axis duplicates a face axis and is skipped.</summary>
    private const float ParallelEpsilon = 1e-4f;

    /// <summary>
    /// Face axes win over edge axes unless an edge axis is clearly smaller by this much: edge-edge contacts are rarer and noisy, and
    /// preferring faces keeps a resting box from flickering between nearly equal choices.
    /// </summary>
    private const float EdgeBias = 1e-3f;

    /// <summary>Whether two oriented boxes overlap, and the shortest push that separates them.</summary>
    /// <param name="a">The box to push.</param>
    /// <param name="b">The box that stays.</param>
    /// <param name="normal">The unit direction to move <paramref name="a"/> along to separate them (from <paramref name="b"/> toward <paramref name="a"/>).</param>
    /// <param name="depth">How far to move <paramref name="a"/> along <paramref name="normal"/>.</param>
    /// <returns>True when the boxes' interiors overlap (touching faces do not count).</returns>
    public static bool TryPenetration(in Obb3 a, in Obb3 b, out Vector3 normal, out float depth)
    {
        Span<Vector3> axesA = [a.Axis(0), a.Axis(1), a.Axis(2)];
        Span<Vector3> axesB = [b.Axis(0), b.Axis(1), b.Axis(2)];
        var offset = a.Center - b.Center;

        float bestDepth = float.MaxValue;
        var bestNormal = Vector3.UnitY;
        bool bestIsFace = false;

        for (int i = 0; i < 6; i++)
        {
            var axis = i < 3 ? axesA[i] : axesB[i - 3];
            if (!TryAxis(axis, offset, a.HalfExtents, axesA, b.HalfExtents, axesB, out float overlap, out var direction))
            {
                normal = default;
                depth = 0f;
                return false;
            }

            if (overlap < bestDepth)
            {
                bestDepth = overlap;
                bestNormal = direction;
                bestIsFace = true;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var cross = Vector3.Cross(axesA[i], axesB[j]);
                float length = cross.Length();
                if (length < ParallelEpsilon) continue;

                if (!TryAxis(cross / length, offset, a.HalfExtents, axesA, b.HalfExtents, axesB, out float overlap, out var direction))
                {
                    normal = default;
                    depth = 0f;
                    return false;
                }

                if (overlap < bestDepth - (bestIsFace ? EdgeBias : 0f))
                {
                    bestDepth = overlap;
                    bestNormal = direction;
                    bestIsFace = false;
                }
            }
        }

        normal = bestNormal;
        depth = bestDepth;
        return true;
    }

    /// <summary>Tests one axis: how much the two boxes' shadows on it overlap, and which way along it <c>a</c> must move to leave <c>b</c>.</summary>
    private static bool TryAxis(
        Vector3 axis, Vector3 offset, Vector3 halfA, ReadOnlySpan<Vector3> axesA, Vector3 halfB, ReadOnlySpan<Vector3> axesB,
        out float overlap, out Vector3 direction)
    {
        float radiusA = halfA.X * MathF.Abs(Vector3.Dot(axis, axesA[0])) + halfA.Y * MathF.Abs(Vector3.Dot(axis, axesA[1])) + halfA.Z * MathF.Abs(Vector3.Dot(axis, axesA[2]));
        float radiusB = halfB.X * MathF.Abs(Vector3.Dot(axis, axesB[0])) + halfB.Y * MathF.Abs(Vector3.Dot(axis, axesB[1])) + halfB.Z * MathF.Abs(Vector3.Dot(axis, axesB[2]));
        float distance = Vector3.Dot(offset, axis);

        overlap = radiusA + radiusB - MathF.Abs(distance);
        direction = distance >= 0f ? axis : -axis;
        return overlap > 0f;
    }
}
