using System.Numerics;
using Engine.Core;

namespace Engine.Physics3D;

// Ray queries against the static boxes.
public sealed partial class CollisionSolver3D
{
    /// <summary>Finds the nearest static box a ray hits within <paramref name="maxDistance"/>.</summary>
    /// <param name="ray">The ray (unit direction).</param>
    /// <param name="maxDistance">How far along the ray to look.</param>
    /// <param name="hit">The nearest hit; default when nothing was hit.</param>
    /// <returns>True when a box was hit.</returns>
    public bool Raycast(in Ray ray, float maxDistance, out RaycastHit3D hit)
    {
        hit = default;
        float nearest = maxDistance;
        bool found = false;

        for (int i = 0; i < StaticBoxes.Count; i++)
        {
            var box = StaticBoxes[i];
            if (!ray.IntersectsBox(box.Min, box.Max, out float distance) || distance > nearest) continue;

            nearest = distance;
            found = true;
            var point = ray.GetPoint(distance);
            hit = new RaycastHit3D(distance, point, FaceNormal(box, point, ray), i);
        }

        return found;
    }

    /// <summary>
    /// The normal of the face a hit point lies on: the axis along which the point is furthest out, relative to the box's half-size
    /// (so a long thin box still picks the right face). A ray that starts inside the box has no entry face, so it gets the reverse of its direction.
    /// </summary>
    private static Vector3 FaceNormal(in Aabb3 box, Vector3 point, in Ray ray)
    {
        var half = box.Size * 0.5f;
        var local = point - box.Center;
        if (box.Min.X < ray.Origin.X && ray.Origin.X < box.Max.X
            && box.Min.Y < ray.Origin.Y && ray.Origin.Y < box.Max.Y
            && box.Min.Z < ray.Origin.Z && ray.Origin.Z < box.Max.Z)
            return -ray.Direction;

        float x = half.X > 0f ? MathF.Abs(local.X) / half.X : 0f;
        float y = half.Y > 0f ? MathF.Abs(local.Y) / half.Y : 0f;
        float z = half.Z > 0f ? MathF.Abs(local.Z) / half.Z : 0f;
        if (x >= y && x >= z) return new Vector3(MathF.Sign(local.X), 0f, 0f);
        return y >= z ? new Vector3(0f, MathF.Sign(local.Y), 0f) : new Vector3(0f, 0f, MathF.Sign(local.Z));
    }
}
