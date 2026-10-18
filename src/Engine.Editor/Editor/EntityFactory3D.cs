using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics3D;
using Engine.Rendering;
using Engine.Rendering3D;

namespace Engine.Editor;

/// <summary>Builds the entities of the editor's Edit &gt; Add 3D menu: boxes (optionally solid, for level geometry) and lights, placed on a grid.</summary>
public static class EntityFactory3D
{
    /// <summary>Rounds each coordinate to the nearest multiple of <paramref name="step"/>, so level pieces line up.</summary>
    /// <param name="point">The point to snap.</param>
    /// <param name="step">The grid cell size; zero or less returns the point unchanged.</param>
    public static Vector3 Snap(Vector3 point, float step) =>
        step <= 0f ? point : new Vector3(MathF.Round(point.X / step) * step, MathF.Round(point.Y / step) * step, MathF.Round(point.Z / step) * step);

    /// <summary>
    /// Creates a drawn box (or any mesh of unit size). With <paramref name="solid"/> it is also level geometry: a <see cref="BoxCollider3D"/> of the same
    /// size plus <see cref="StaticSolid3D"/>, so the character controller walks on it. The collider ignores later scale changes (its size is its own field).
    /// </summary>
    /// <param name="world">The world to create it in.</param>
    /// <param name="name">The entity's name.</param>
    /// <param name="position">World position of the centre.</param>
    /// <param name="size">Size in metres; the transform's scale, since the mesh is one unit across.</param>
    /// <param name="mesh">The mesh to draw.</param>
    /// <param name="material">The material, or an invalid handle for the default.</param>
    /// <param name="solid">Whether to make it static collision geometry.</param>
    public static Entity Box(World world, string name, Vector3 position, Vector3 size, AssetHandle<Mesh> mesh, AssetHandle<Material> material, bool solid)
    {
        var entity = world.CreateEntity();
        world.Add(entity, Name.Of(name));
        world.Add(entity, new Transform { Position = position, Rotation = Quaternion.Identity, Scale = size });
        world.Add(entity, MeshRenderer.Create(mesh, material));
        if (solid)
        {
            world.Add(entity, new BoxCollider3D { Size = size });
            world.Add(entity, new StaticSolid3D());
        }

        return entity;
    }

    /// <summary>Creates a warm point light reaching <paramref name="radius"/> metres.</summary>
    public static Entity PointLight(World world, Vector3 position, float radius = 8f)
    {
        var entity = world.CreateEntity();
        world.Add(entity, Name.Of("Point light"));
        world.Add(entity, Transform.At(position));
        world.Add(entity, PointLight3D.Create(radius, new Color(1f, 0.85f, 0.6f), 1.5f));
        return entity;
    }
}
