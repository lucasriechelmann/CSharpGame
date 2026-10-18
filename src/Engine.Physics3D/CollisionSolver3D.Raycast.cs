using System.Numerics;
using Engine.Core;

namespace Engine.Physics3D;

// Ray queries against the static boxes, the oriented static boxes and the moving bodies.
public sealed partial class CollisionSolver3D
{
    /// <summary>Finds the nearest box a ray hits within <paramref name="maxDistance"/>: a static solid, an oriented static solid, or a moving body.</summary>
    /// <param name="ray">The ray (unit direction).</param>
    /// <param name="maxDistance">How far along the ray to look.</param>
    /// <param name="hit">The nearest hit; default when nothing was hit. Its index counts <see cref="StaticBoxes"/> first, then <see cref="StaticObbs"/>, then <see cref="DynamicBoxes"/>.</param>
    /// <returns>True when a box was hit.</returns>
    public bool Raycast(in Ray ray, float maxDistance, out RaycastHit3D hit)
    {
        hit = default;
        float nearest = maxDistance;
        bool found = false;

        for (int i = 0; i < StaticBoxes.Count; i++) found |= TryBox(ray, StaticBoxes[i], i, ref nearest, ref hit);

        int obbBase = StaticBoxes.Count;
        for (int i = 0; i < StaticObbs.Count; i++) found |= TryObb(ray, StaticObbs[i], obbBase + i, ref nearest, ref hit);

        int dynamicBase = obbBase + StaticObbs.Count;
        for (int i = 0; i < DynamicBoxes.Count; i++) found |= TryBox(ray, DynamicBoxes[i], dynamicBase + i, ref nearest, ref hit);

        return found;
    }

    private static bool TryBox(in Ray ray, in Aabb3 box, int index, ref float nearest, ref RaycastHit3D hit)
    {
        if (!ray.IntersectsBox(box.Min, box.Max, out float distance) || distance > nearest) return false;

        nearest = distance;
        var point = ray.GetPoint(distance);
        hit = new RaycastHit3D(distance, point, FaceNormal(box, point, ray), index);
        return true;
    }

    /// <summary>An oriented box is hit by moving the ray into the box's own space, where it is an ordinary box, and turning the normal back.</summary>
    private static bool TryObb(in Ray ray, in Obb3 obb, int index, ref float nearest, ref RaycastHit3D hit)
    {
        var inverse = Quaternion.Inverse(obb.Rotation);
        var local = new Ray(obb.ToLocal(ray.Origin), Vector3.Transform(ray.Direction, inverse)); // a rotation keeps the direction unit length, so distances are unchanged
        var box = obb.LocalBox;
        if (!local.IntersectsBox(box.Min, box.Max, out float distance) || distance > nearest) return false;

        nearest = distance;
        hit = new RaycastHit3D(distance, ray.GetPoint(distance), obb.ToWorldDirection(FaceNormal(box, local.GetPoint(distance), local)), index);
        return true;
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
