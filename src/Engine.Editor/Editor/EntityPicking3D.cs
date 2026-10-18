using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Rendering3D;

namespace Engine.Editor;

/// <summary>
/// Answers "which entity is under this mouse ray?" for the 3D editor view, and gives the box corners to outline the selection with.
/// Pure maths over the world and a camera (no ImGui, no GL), so it is unit-tested; what an entity's boxes are comes from a caller-supplied
/// source because the meshes live in the asset manager.
/// </summary>
/// <remarks>
/// An entity is hit where its mesh's bounding box is (oriented with the entity, so a rotated crate is hit where the crate is). An entity
/// with no box (a light, a marker, an empty) has no volume, so it is hit within a small radius that stays the same size on screen.
/// The nearest hit along the ray wins. Boxes are coarser than the real triangles; that is the usual editor trade (fast and forgiving).
/// </remarks>
public static class EntityPicking3D
{
    /// <summary>Radius, in screen pixels, within which an entity without a box counts as clicked.</summary>
    public const float FallbackRadiusPixels = 12f;

    /// <summary>Fills <paramref name="boxes"/> with the boxes of one entity (none for an entity that draws nothing).</summary>
    /// <param name="entity">The entity to describe.</param>
    /// <param name="boxes">Receives the boxes; empty on entry.</param>
    public delegate void BoxSource(Entity entity, List<PickBox> boxes);

    /// <summary>The entity the ray hits first.</summary>
    /// <param name="world">The world to search.</param>
    /// <param name="ray">The mouse ray in world space (<c>Camera3D.ScreenPointToRay</c>).</param>
    /// <param name="camera">Used to size the no-volume hit radius.</param>
    /// <param name="boxSource">Where each entity's boxes come from.</param>
    /// <param name="distance">Distance along the ray to the hit.</param>
    /// <returns>The entity, or <see cref="Entity.Null"/> when the ray hits nothing.</returns>
    public static Entity Pick(World world, Ray ray, Camera3D camera, BoxSource boxSource, out float distance)
    {
        var best = Entity.Null;
        float bestDistance = float.PositiveInfinity;
        var boxes = new List<PickBox>();

        foreach (var entity in world.Entities)
        {
            if (!world.Has<Transform>(entity)) continue;

            boxes.Clear();
            boxSource(entity, boxes);

            float hit = float.PositiveInfinity;
            if (boxes.Count > 0)
            {
                foreach (var box in boxes)
                {
                    if (TryHitBox(ray, box, out float t) && t < hit) hit = t;
                }
            }
            else
            {
                var position = TransformHierarchy.GetWorldPosition(world, entity, world.Get<Transform>(entity));
                float radius = FallbackRadiusPixels * WorldUnitsPerPixel(camera, position);
                if (ray.IntersectsSphere(position, radius, out float t)) hit = t;
            }

            if (hit < bestDistance)
            {
                bestDistance = hit;
                best = entity;
            }
        }

        distance = best.IsNull ? 0f : bestDistance;
        return best;
    }

    /// <summary>
    /// Distance along the ray to where it enters the oriented box. The ray is moved into the box's own space, where the box is axis-aligned,
    /// and tested there; the direction is deliberately not renormalised, so the distance stays in world units.
    /// </summary>
    /// <param name="ray">The ray in world space.</param>
    /// <param name="box">The box.</param>
    /// <param name="distance">Distance along the world ray (0 when the origin is inside).</param>
    public static bool TryHitBox(Ray ray, PickBox box, out float distance)
    {
        distance = 0f;
        if (!Matrix4x4.Invert(box.LocalToWorld, out var toLocal)) return false; // a flattened (zero-scale) box has no volume to hit

        var origin = Vector3.Transform(ray.Origin, toLocal);
        var direction = Vector3.TransformNormal(ray.Direction, toLocal);
        return new Ray(origin, direction).IntersectsBox(box.Min, box.Max, out distance);
    }

    /// <summary>How many world units one screen pixel spans at <paramref name="point"/>, for sizes that should stay constant on screen.</summary>
    /// <param name="camera">The viewing camera.</param>
    /// <param name="point">The world point (its distance along the view direction sets the scale).</param>
    public static float WorldUnitsPerPixel(Camera3D camera, Vector3 point)
    {
        float depth = MathF.Max(Vector3.Dot(point - camera.Position, camera.Forward), camera.Near);
        return 2f * depth * MathF.Tan(camera.FovY * 0.5f) / MathF.Max(camera.ViewportSize.Y, 1f);
    }

    /// <summary>Writes the eight corners of a box in world space; corners 0-3 are the min-Z face, 4-7 the max-Z face, each counter-clockwise from min X, min Y.</summary>
    /// <param name="box">The box.</param>
    /// <param name="corners">Receives eight points.</param>
    public static void GetCorners(PickBox box, Span<Vector3> corners)
    {
        if (corners.Length < 8) throw new ArgumentException("Eight corners are needed.", nameof(corners));

        var (min, max) = (box.Min, box.Max);
        Span<Vector3> local =
        [
            new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z),
        ];
        for (int i = 0; i < 8; i++) corners[i] = Vector3.Transform(local[i], box.LocalToWorld);
    }
}
