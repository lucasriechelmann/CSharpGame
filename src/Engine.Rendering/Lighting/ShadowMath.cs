using System.Numerics;

namespace Engine.Rendering;

/// <summary>The pure geometry of 2D shadows: which walls cast, and the quad of darkness each one throws away from a light. No GL.</summary>
public static class ShadowMath
{
    /// <summary>True when the wall faces away from the light, which is the only kind that casts a shadow (a wall facing the light is lit, not shadowed).</summary>
    /// <param name="light">The light's position.</param>
    /// <param name="edge">The wall.</param>
    public static bool FacesAwayFrom(Vector2 light, in ShadowEdge edge) =>
        Vector2.Dot(edge.OutwardNormal, light - edge.A) < 0f;

    /// <summary>True when the wall passes within <paramref name="radius"/> of the light, so it can matter at all.</summary>
    /// <param name="light">The light's position.</param>
    /// <param name="radius">How far the light reaches.</param>
    /// <param name="edge">The wall.</param>
    public static bool InRange(Vector2 light, float radius, in ShadowEdge edge)
    {
        var ab = edge.B - edge.A;
        float lengthSquared = ab.LengthSquared();
        float t = lengthSquared > 0f ? Math.Clamp(Vector2.Dot(light - edge.A, ab) / lengthSquared, 0f, 1f) : 0f;
        var closest = edge.A + ab * t;
        return Vector2.DistanceSquared(light, closest) <= radius * radius;
    }

    /// <summary>
    /// Builds the shadow a wall throws: the wall's two end points and the same two points pushed away from the light along the rays
    /// from the light through them. Everything between those rays beyond the wall is in shadow.
    /// </summary>
    /// <param name="light">The light's position.</param>
    /// <param name="edge">The wall.</param>
    /// <param name="distance">How far to push the points; use at least twice the light's radius so the shadow reaches past the light's glow.</param>
    /// <param name="quad">Receives the four corners in order A, B, pushed B, pushed A.</param>
    /// <returns>False when an end point sits on the light itself (no direction to push along), in which case <paramref name="quad"/> is not valid.</returns>
    public static bool TryExtrude(Vector2 light, in ShadowEdge edge, float distance, Span<Vector2> quad)
    {
        var fromA = edge.A - light;
        var fromB = edge.B - light;
        float lengthA = fromA.Length();
        float lengthB = fromB.Length();
        if (lengthA < 1e-5f || lengthB < 1e-5f) return false;

        quad[0] = edge.A;
        quad[1] = edge.B;
        quad[2] = edge.B + fromB / lengthB * distance;
        quad[3] = edge.A + fromA / lengthA * distance;
        return true;
    }

    /// <summary>
    /// Fills <paramref name="triangles"/> with the shadow of every wall that casts one from this light (two triangles per wall) and returns how many
    /// vertices were written. Walls out of range or facing the light are skipped.
    /// </summary>
    /// <param name="light">The light's position.</param>
    /// <param name="radius">How far the light reaches.</param>
    /// <param name="edges">The candidate walls.</param>
    /// <param name="triangles">Output vertices, cleared first.</param>
    public static int BuildShadowTriangles(Vector2 light, float radius, List<ShadowEdge> edges, List<Vector2> triangles)
    {
        triangles.Clear();
        Span<Vector2> quad = stackalloc Vector2[4];
        float distance = radius * 2f;
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            if (!InRange(light, radius, edge) || !FacesAwayFrom(light, edge) || !TryExtrude(light, edge, distance, quad)) continue;

            triangles.Add(quad[0]);
            triangles.Add(quad[1]);
            triangles.Add(quad[2]);
            triangles.Add(quad[2]);
            triangles.Add(quad[3]);
            triangles.Add(quad[0]);
        }

        return triangles.Count;
    }
}
