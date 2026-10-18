using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>A half-line: an origin and a unit direction. The basis of picking and, later, raycasts against physics shapes.</summary>
/// <param name="Origin">Where the ray starts.</param>
/// <param name="Direction">Unit-length direction.</param>
public readonly record struct Ray(Vector3 Origin, Vector3 Direction)
{
    /// <summary>The point <paramref name="distance"/> along the ray.</summary>
    public Vector3 GetPoint(float distance) => Origin + Direction * distance;

    /// <summary>Distance to where the ray first enters a sphere, or false when it misses (or the sphere is entirely behind).</summary>
    /// <param name="centre">Sphere centre.</param>
    /// <param name="radius">Sphere radius.</param>
    /// <param name="distance">Distance along the ray (0 when the origin is inside the sphere).</param>
    public bool IntersectsSphere(Vector3 centre, float radius, out float distance)
    {
        var toCentre = centre - Origin;
        float along = Vector3.Dot(toCentre, Direction);
        float closestSquared = toCentre.LengthSquared() - along * along;
        float radiusSquared = radius * radius;
        distance = 0f;
        if (closestSquared > radiusSquared) return false;

        float half = MathF.Sqrt(radiusSquared - closestSquared);
        float entry = along - half;
        float exit = along + half;
        if (exit < 0f) return false; // sphere is behind the origin
        distance = MathF.Max(entry, 0f);
        return true;
    }

    /// <summary>Distance to where the ray enters an axis-aligned box (slab method), or false on a miss.</summary>
    /// <param name="min">Smallest corner of the box.</param>
    /// <param name="max">Largest corner of the box.</param>
    /// <param name="distance">Distance along the ray (0 when the origin is inside the box).</param>
    public bool IntersectsBox(Vector3 min, Vector3 max, out float distance)
    {
        float tMin = 0f;
        float tMax = float.MaxValue;
        for (int axis = 0; axis < 3; axis++)
        {
            float origin = axis == 0 ? Origin.X : axis == 1 ? Origin.Y : Origin.Z;
            float direction = axis == 0 ? Direction.X : axis == 1 ? Direction.Y : Direction.Z;
            float lo = axis == 0 ? min.X : axis == 1 ? min.Y : min.Z;
            float hi = axis == 0 ? max.X : axis == 1 ? max.Y : max.Z;

            if (MathF.Abs(direction) < 1e-8f)
            {
                // Parallel to this pair of faces: a hit is only possible when the origin lies between them.
                if (origin < lo || origin > hi)
                {
                    distance = 0f;
                    return false;
                }

                continue;
            }

            float t1 = (lo - origin) / direction;
            float t2 = (hi - origin) / direction;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax)
            {
                distance = 0f;
                return false;
            }
        }

        distance = tMin;
        return true;
    }
}
