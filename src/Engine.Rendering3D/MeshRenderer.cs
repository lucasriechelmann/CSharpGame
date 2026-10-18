using Engine.Core.Assets;

namespace Engine.Rendering3D;

/// <summary>
/// Draws a <see cref="Mesh"/> with a <see cref="Material"/> at the entity's <c>Transform</c>. Plain data: the assets are
/// referenced by handle (saved as names in scenes), and <see cref="MeshRenderSystem"/> does the drawing.
/// </summary>
public struct MeshRenderer
{
    /// <summary>The geometry. An invalid handle draws nothing.</summary>
    public AssetHandle<Mesh> Mesh;

    /// <summary>The surface look. An invalid handle draws the mesh in plain white.</summary>
    public AssetHandle<Material> Material;

    /// <summary>Creates a renderer.</summary>
    public static MeshRenderer Create(AssetHandle<Mesh> mesh, AssetHandle<Material> material) => new() { Mesh = mesh, Material = material };
}
