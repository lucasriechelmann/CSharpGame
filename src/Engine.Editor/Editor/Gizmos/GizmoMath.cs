using System.Numerics;
using Engine.Core;

namespace Engine.Editor.Gizmos;

/// <summary>The small geometric questions a gizmo drag asks of the mouse ray. Pure, so each is unit-tested on its own.</summary>
public static class GizmoMath
{
    /// <summary>
    /// Where along an axis line the mouse ray passes closest: the parameter <c>s</c> of the point <c>origin + s * direction</c> nearest to
    /// the ray (the closest points of two skew lines). Dragging an arrow moves the entity so that point stays under the mouse.
    /// </summary>
    /// <param name="ray">The mouse ray.</param>
    /// <param name="axisOrigin">A point on the axis.</param>
    /// <param name="axisDirection">The unit direction of the axis.</param>
    /// <param name="s">The distance along the axis from <paramref name="axisOrigin"/>.</param>
    /// <returns>False when the ray is (almost) parallel to the axis, where the question has no useful answer.</returns>
    public static bool TryClosestOnAxis(Ray ray, Vector3 axisOrigin, Vector3 axisDirection, out float s)
    {
        var offset = axisOrigin - ray.Origin;
        float b = Vector3.Dot(axisDirection, ray.Direction);
        float d = Vector3.Dot(axisDirection, offset);
        float e = Vector3.Dot(ray.Direction, offset);
        float denominator = 1f - b * b; // both directions are unit length
        if (denominator < 1e-6f)
        {
            s = 0f;
            return false;
        }

        s = (b * e - d) / denominator;
        return true;
    }

    /// <summary>Where a ray crosses the plane through <paramref name="point"/> with the given normal.</summary>
    /// <param name="ray">The ray.</param>
    /// <param name="point">A point on the plane.</param>
    /// <param name="normal">The plane's normal.</param>
    /// <param name="hit">The crossing point.</param>
    /// <returns>False when the ray runs parallel to the plane or the plane lies behind the ray's origin.</returns>
    public static bool TryIntersectPlane(Ray ray, Vector3 point, Vector3 normal, out Vector3 hit)
    {
        float denominator = Vector3.Dot(normal, ray.Direction);
        if (MathF.Abs(denominator) < 1e-6f)
        {
            hit = default;
            return false;
        }

        float t = Vector3.Dot(normal, point - ray.Origin) / denominator;
        hit = ray.GetPoint(t);
        return t >= 0f;
    }

    /// <summary>The signed angle, in radians, that turns <paramref name="from"/> into <paramref name="to"/> about <paramref name="axis"/> (counter-clockwise seen from the tip of the axis is positive).</summary>
    /// <param name="from">The first vector (perpendicular to the axis).</param>
    /// <param name="to">The second vector (perpendicular to the axis).</param>
    /// <param name="axis">The unit rotation axis.</param>
    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis) =>
        MathF.Atan2(Vector3.Dot(Vector3.Cross(from, to), axis), Vector3.Dot(from, to));

    /// <summary>The distance in pixels from a point to a line segment (to the nearer end when the point is beside neither).</summary>
    /// <param name="point">The point.</param>
    /// <param name="a">One end of the segment.</param>
    /// <param name="b">The other end.</param>
    public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var edge = b - a;
        float lengthSquared = edge.LengthSquared();
        if (lengthSquared < 1e-8f) return Vector2.Distance(point, a);

        float t = Math.Clamp(Vector2.Dot(point - a, edge) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, a + edge * t);
    }

    /// <summary>Rounds <paramref name="value"/> to the nearest multiple of <paramref name="step"/>; a step of zero or less leaves it alone.</summary>
    /// <param name="value">The value to snap.</param>
    /// <param name="step">The grid size.</param>
    public static float Snap(float value, float step) => step > 0f ? MathF.Round(value / step) * step : value;
}
