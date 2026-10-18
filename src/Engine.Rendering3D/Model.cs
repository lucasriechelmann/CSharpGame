using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

/// <summary>
/// A multi-part 3D model on the GPU (what a glTF file becomes): meshes, materials and the textures embedded in the file, all owned by
/// the model and released with it. A loaded <c>AssetManager</c> asset; entities show it with a <see cref="ModelRenderer"/>.
/// </summary>
public sealed class Model : IDisposable
{
    private readonly List<Texture2D> _textures = [];

    /// <summary>Uploads the data. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the buffers and textures.</param>
    /// <param name="data">The imported model.</param>
    public Model(GL gl, ModelData data)
    {
        var materials = new Material[data.Materials.Count];
        for (int i = 0; i < materials.Length; i++)
        {
            var source = data.Materials[i];
            Texture2D? texture = null;
            if (source.Image != null)
            {
                texture = Texture2D.FromImageBytes(gl, source.Image, TextureFilter.Trilinear);
                texture.SetRepeat(); // glTF samplers default to repeat
                _textures.Add(texture);
            }

            materials[i] = new Material
            {
                BaseColor = source.BaseColor,
                OwnedTexture = texture,
                Shininess = source.Shininess,
                Specular = source.Specular,
                DoubleSided = source.DoubleSided,
            };
        }

        var parts = new ModelPart[data.Parts.Count];
        for (int i = 0; i < parts.Length; i++)
        {
            var part = data.Parts[i];
            parts[i] = new ModelPart(new Mesh(gl, part.Mesh), materials[part.MaterialIndex], part.Local, part.Node);
        }

        Parts = parts;
        Nodes = data.Nodes;
    }

    /// <summary>The node tree the pieces came from (parents before children), for posing and animation; empty for a hand-built model.</summary>
    public IReadOnlyList<ModelNodeData> Nodes { get; }

    /// <summary>The pieces to draw.</summary>
    public IReadOnlyList<ModelPart> Parts { get; }

    /// <summary>Deletes every mesh and embedded texture.</summary>
    public void Dispose()
    {
        foreach (var part in Parts) part.Mesh.Dispose();
        foreach (var texture in _textures) texture.Dispose();
    }
}
