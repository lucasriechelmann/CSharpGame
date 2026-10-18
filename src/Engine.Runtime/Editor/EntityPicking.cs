using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Engine.Runtime.Editor;

/// <summary>
/// Answers "which entity is under this world point?" for click-to-select, and gives the outline to draw around the selection.
/// Pure maths over the world (no ImGui, no GL), so it is unit-tested.
/// </summary>
/// <remarks>
/// An entity with a <see cref="Sprite"/> is hit where its (scaled, rotated) quad is. An entity with only a <c>Transform</c>
/// (a light, a marker) has no area, so it is hit within a small radius of its position. Tile layers are skipped: they cover
/// the whole level and would swallow every click, so they are selected from the hierarchy instead.
/// </remarks>
public static class EntityPicking
{
    /// <summary>Radius, in screen pixels, within which an entity without a sprite counts as clicked.</summary>
    public const float FallbackRadiusPixels = 12f;

    /// <summary>
    /// The topmost entity under <paramref name="worldPoint"/>: the largest Z wins (it draws in front), then the smaller quad
    /// (so a small thing sitting on a big one can still be picked).
    /// </summary>
    /// <param name="world">The world to search.</param>
    /// <param name="worldPoint">The point in world space.</param>
    /// <param name="fallbackRadius">Hit radius in world units for entities without a sprite (<see cref="FallbackRadiusPixels"/> divided by the camera zoom).</param>
    /// <returns>The entity, or <see cref="Entity.Null"/> when nothing is there.</returns>
    public static Entity Pick(World world, Vector2 worldPoint, float fallbackRadius)
    {
        var best = Entity.Null;
        float bestZ = float.NegativeInfinity;
        float bestArea = float.PositiveInfinity;

        foreach (var entity in world.Entities)
        {
            if (!world.TryGet<Transform>(entity, out var transform) || world.Has<TileLayerRenderer>(entity)) continue;

            float area;
            if (world.TryGet<Sprite>(entity, out var sprite))
            {
                var size = new Vector2(sprite.Size.X * transform.Scale.X, sprite.Size.Y * transform.Scale.Y);
                if (!QuadContains(transform, size, sprite.Pivot, worldPoint)) continue;
                area = MathF.Abs(size.X * size.Y);
            }
            else
            {
                var position = new Vector2(transform.Position.X, transform.Position.Y);
                if (Vector2.DistanceSquared(position, worldPoint) > fallbackRadius * fallbackRadius) continue;
                area = 0f;
            }

            float z = transform.Position.Z;
            if (z > bestZ || (z == bestZ && area < bestArea))
            {
                best = entity;
                bestZ = z;
                bestArea = area;
            }
        }

        return best;
    }

    /// <summary>
    /// Writes the four corners of the entity's sprite quad in world space (counter-clockwise from the bottom-left as drawn) for
    /// the selection outline.
    /// </summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="entity">The entity to outline.</param>
    /// <param name="corners">Receives four points; must hold at least four.</param>
    /// <returns>False when the entity has no sprite (nothing to outline).</returns>
    public static bool TryGetOutline(World world, Entity entity, Span<Vector2> corners)
    {
        if (corners.Length < 4) throw new ArgumentException("Four corners are needed.", nameof(corners));
        if (!world.IsAlive(entity) || !world.TryGet<Transform>(entity, out var transform) || !world.TryGet<Sprite>(entity, out var sprite))
            return false;

        var size = new Vector2(sprite.Size.X * transform.Scale.X, sprite.Size.Y * transform.Scale.Y);
        var (min, max) = LocalRect(size, sprite.Pivot);
        float cos = MathF.Cos(transform.RotationZ);
        float sin = MathF.Sin(transform.RotationZ);
        var origin = new Vector2(transform.Position.X, transform.Position.Y);

        Span<Vector2> local = [min, new Vector2(max.X, min.Y), max, new Vector2(min.X, max.Y)];
        for (int i = 0; i < 4; i++)
            corners[i] = origin + new Vector2(local[i].X * cos - local[i].Y * sin, local[i].X * sin + local[i].Y * cos);

        return true;
    }

    /// <summary>True when <paramref name="worldPoint"/> is inside the quad of a sprite of <paramref name="size"/> placed at <paramref name="transform"/>.</summary>
    /// <param name="transform">Position and rotation of the entity.</param>
    /// <param name="size">Quad size after the transform's scale.</param>
    /// <param name="pivot">Pivot as a fraction of the size from the top-left (the point the position and rotation act on).</param>
    /// <param name="worldPoint">The point to test.</param>
    public static bool QuadContains(in Transform transform, Vector2 size, Vector2 pivot, Vector2 worldPoint)
    {
        // Undo the rotation around the position so the test is against an axis-aligned rectangle.
        var offset = worldPoint - new Vector2(transform.Position.X, transform.Position.Y);
        float cos = MathF.Cos(-transform.RotationZ);
        float sin = MathF.Sin(-transform.RotationZ);
        var local = new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);

        var (min, max) = LocalRect(size, pivot);
        return local.X >= min.X && local.X <= max.X && local.Y >= min.Y && local.Y <= max.Y;
    }

    /// <summary>The quad's rectangle relative to the entity position (Y up): pivot (0.5, 0.5) centres it, (0, 0) puts the top-left corner on the position.</summary>
    private static (Vector2 Min, Vector2 Max) LocalRect(Vector2 size, Vector2 pivot)
    {
        var min = new Vector2(-pivot.X * size.X, -(1f - pivot.Y) * size.Y);
        var max = new Vector2((1f - pivot.X) * size.X, pivot.Y * size.Y);
        return (Vector2.Min(min, max), Vector2.Max(min, max)); // a negative scale flips the corners
    }
}
