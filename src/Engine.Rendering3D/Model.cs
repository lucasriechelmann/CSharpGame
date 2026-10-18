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
            materials[i] = new Material
            {
                BaseColor = source.BaseColor,
                OwnedTexture = Upload(gl, source.Image),
                Metallic = source.Metallic,
                Roughness = source.Roughness,
                OwnedMetallicRoughnessTexture = Upload(gl, source.MetallicRoughnessImage),
                OwnedNormalTexture = Upload(gl, source.NormalImage),
                NormalScale = source.NormalScale,
                Emissive = source.Emissive,
                AlphaMode = source.AlphaMode,
                AlphaCutoff = source.AlphaCutoff,
                DoubleSided = source.DoubleSided,
            };
        }

        var parts = new ModelPart[data.Parts.Count];
        for (int i = 0; i < parts.Length; i++)
        {
            var part = data.Parts[i];
            parts[i] = new ModelPart(new Mesh(gl, part.Mesh), materials[part.MaterialIndex], part.Local, part.Node, part.Skin);
        }

        Parts = parts;
        Nodes = data.Nodes;
        Skins = data.Skins;
        Animations = data.Animations;
    }

    /// <summary>Decodes an embedded image into a texture the model owns (repeat wrapping, like glTF samplers); null when the material has none.</summary>
    private Texture2D? Upload(GL gl, byte[]? image)
    {
        if (image == null) return null;

        var texture = Texture2D.FromImageBytes(gl, image, TextureFilter.Trilinear);
        texture.SetRepeat(); // glTF samplers default to repeat
        _textures.Add(texture);
        return texture;
    }

    /// <summary>The node tree the pieces came from (parents before children), for posing and animation; empty for a hand-built model.</summary>
    public IReadOnlyList<ModelNodeData> Nodes { get; }

    /// <summary>The skins skinned parts refer to (<see cref="ModelPart.Skin"/>); empty for a rigid model.</summary>
    public IReadOnlyList<ModelSkinData> Skins { get; }

    /// <summary>The animation clips; empty for a static model.</summary>
    public IReadOnlyList<ModelAnimationData> Animations { get; }

    /// <summary>The pieces to draw.</summary>
    public IReadOnlyList<ModelPart> Parts { get; }

    /// <summary>Deletes every mesh and embedded texture.</summary>
    public void Dispose()
    {
        foreach (var part in Parts) part.Mesh.Dispose();
        foreach (var texture in _textures) texture.Dispose();
    }
}
