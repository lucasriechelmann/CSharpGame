using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Rendering3D;

namespace Engine.Editor;

/// <summary>
/// Supplies <see cref="EntityPicking3D"/> with the boxes of what an entity draws: the bounds of its <see cref="MeshRenderer"/> mesh, or
/// of each part of its <see cref="ModelRenderer"/> model, placed by the entity's world matrix. Needs the asset manager to resolve handles.
/// </summary>
internal sealed class RenderedBoxes(World world, AssetManager assets)
{
    /// <summary>Adds the boxes of <paramref name="entity"/> to <paramref name="boxes"/>; an entity that draws no mesh adds none.</summary>
    /// <param name="entity">The entity to describe.</param>
    /// <param name="boxes">Receives the boxes.</param>
    public void Collect(Entity entity, List<PickBox> boxes)
    {
        bool hasMesh = world.TryGet<MeshRenderer>(entity, out var meshRenderer);
        bool hasModel = world.TryGet<ModelRenderer>(entity, out var modelRenderer);
        if (!hasMesh && !hasModel) return;

        var entityMatrix = TransformHierarchy.GetWorldMatrix(world, entity);
        if (hasMesh && meshRenderer.Mesh.IsValid && assets.TryGet(meshRenderer.Mesh, out var mesh))
            boxes.Add(new PickBox(entityMatrix, mesh.Data.BoundsMin, mesh.Data.BoundsMax));

        if (hasModel && modelRenderer.Model.IsValid && assets.TryGet(modelRenderer.Model, out var model))
        {
            foreach (var part in model.Parts)
                boxes.Add(new PickBox(part.Local * entityMatrix, part.Mesh.Data.BoundsMin, part.Mesh.Data.BoundsMax)); // piece into model space, then the entity places the model
        }
    }
}
