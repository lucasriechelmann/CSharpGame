using Engine.Core.Assets;
using Engine.Rendering3D.Gltf;
using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

/// <summary>Asset loader that reads <c>.gltf</c> and <c>.glb</c> files into GPU <see cref="Model"/>s.</summary>
/// <param name="gl">The GL context that will own the model's buffers and textures.</param>
public sealed class ModelLoader(GL gl) : IAssetLoader<Model>
{
    /// <inheritdoc />
    public Model Load(string fullPath) => new(gl, GltfImporter.ImportFile(fullPath));
}
