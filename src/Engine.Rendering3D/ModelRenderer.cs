using Engine.Core.Assets;

namespace Engine.Rendering3D;

/// <summary>
/// Draws a whole <see cref="Model"/> (every part, with its own material) at the entity's <c>Transform</c>. Plain data: the model is
/// referenced by handle (saved as its file name in scenes), and <see cref="MeshRenderSystem"/> does the drawing.
/// </summary>
public struct ModelRenderer
{
    /// <summary>The model to draw. An invalid handle draws nothing.</summary>
    public AssetHandle<Model> Model;

    /// <summary>Creates a renderer for a model.</summary>
    public static ModelRenderer Create(AssetHandle<Model> model) => new() { Model = model };
}
