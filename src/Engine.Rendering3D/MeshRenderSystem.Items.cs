using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Rendering3D;

// Gathering what is visible this frame into two lists (opaque and cut-out, blended) and sorting them for drawing.
public sealed partial class MeshRenderSystem
{
    // Equal hash codes only cost some batching, never correctness, so identity hashes are a cheap stand-in for "group by material, then by mesh".
    private static readonly Comparison<DrawItem> ByMaterialThenMesh = static (a, b) =>
    {
        int byMaterial = RuntimeHelpers.GetHashCode(a.Material).CompareTo(RuntimeHelpers.GetHashCode(b.Material));
        return byMaterial != 0 ? byMaterial : RuntimeHelpers.GetHashCode(a.Mesh).CompareTo(RuntimeHelpers.GetHashCode(b.Mesh));
    };

    private static readonly Comparison<DrawItem> FarthestFirst = static (a, b) => b.DistanceSquared.CompareTo(a.DistanceSquared);

    private DrawItem[] _opaque = new DrawItem[64];
    private DrawItem[] _blended = new DrawItem[16];
    private int _opaqueCount;
    private int _blendedCount;

    private void GatherItems(World world)
    {
        _opaqueCount = 0;
        _blendedCount = 0;
        world.Query(_drawEntity);
        world.Query(_drawModelEntity);

        // Grouping by material and mesh turns runs of identical things into single instanced draws and saves state changes; blended items must
        // instead be painted back to front, or what is behind would overwrite what is in front.
        _opaque.AsSpan(0, _opaqueCount).Sort(ByMaterialThenMesh);
        _blended.AsSpan(0, _blendedCount).Sort(FarthestFirst);
    }

    private void GatherEntity(Entity entity, ref Transform simulated, ref MeshRenderer renderer)
    {
        if (!renderer.Mesh.IsValid || !_assets.TryGet(renderer.Mesh, out var mesh)) return;

        var material = renderer.Material.IsValid && _assets.TryGet(renderer.Material, out var found) ? found : DefaultMaterial;
        Add(mesh, material, EntityMatrix(entity, simulated));
    }

    private void GatherModelEntity(Entity entity, ref Transform simulated, ref ModelRenderer renderer)
    {
        if (!renderer.Model.IsValid || !_assets.TryGet(renderer.Model, out var model)) return;

        var entityMatrix = EntityMatrix(entity, simulated);
        AnimatedPose? pose = null;
        _poses?.TryGet(entity, out pose);

        var parts = model.Parts;
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.Skin >= 0)
            {
                // A skinned part's vertices are already in model space and its joints place them, so the entity's matrix is all it needs; the
                // pose moves vertices far from where the bind pose would put the bounds, so it is never culled by them.
                Add(part.Mesh, part.Material, entityMatrix, pose?.JointMatrices[part.Skin], cull: false);
            }
            else if (pose != null && part.Node >= 0)
            {
                Add(part.Mesh, part.Material, pose.NodeWorld[part.Node] * entityMatrix); // the part follows its (animated) node
            }
            else
            {
                Add(part.Mesh, part.Material, part.Local * entityMatrix); // piece into model space, then the entity places the model
            }
        }
    }

    /// <summary>The entity's local-to-world matrix, drawn between its last two fixed steps when it has a <see cref="PreviousTransform"/>.</summary>
    private Matrix4x4 EntityMatrix(Entity entity, in Transform simulated)
    {
        if (TransformHierarchy.HasParent(_world, entity)) return TransformHierarchy.GetRenderMatrix(_world, entity, _clock);

        var transform = simulated;
        if (_clock != null && _world.TryGet<PreviousTransform>(entity, out var previous))
            transform = PreviousTransform.Interpolate(simulated, previous, _clock.Alpha);
        return ModelMatrix.From(transform);
    }

    /// <summary>Adds a mesh to the right list unless its bounding sphere is outside the view (skinned meshes pass <c>cull: false</c>).</summary>
    private void Add(Mesh mesh, Material material, in Matrix4x4 model, Matrix4x4[]? joints = null, bool cull = true)
    {
        var centre = Vector3.Transform(mesh.BoundsCenter, model);
        float radius = mesh.BoundsRadius * ModelMatrix.MaxScale(model);
        CollectCaster(mesh, material, model, centre, radius, joints); // casters are not culled by the camera
        if (cull && !_frustum.IntersectsSphere(centre, radius))
        {
            CulledLastFrame++;
            return;
        }

        var item = new DrawItem
        {
            Mesh = mesh,
            Material = material,
            Model = model,
            Joints = joints,
            Centre = centre,
            Radius = radius,
            DistanceSquared = Vector3.DistanceSquared(centre, _camera.Position),
        };

        if (material.IsBlended) Push(ref _blended, ref _blendedCount, item);
        else Push(ref _opaque, ref _opaqueCount, item);
    }

    private static void Push(ref DrawItem[] items, ref int count, in DrawItem item)
    {
        if (count == items.Length) Array.Resize(ref items, count * 2);
        items[count++] = item;
    }
}
