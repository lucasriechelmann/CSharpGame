using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// Asset loader for <c>.material.json</c> files. The textures it names are loaded through the manager (so a texture loader must be
/// registered); the material keeps only the handles.
/// </summary>
/// <param name="assets">The manager that owns the textures.</param>
public sealed class MaterialLoader(AssetManager assets) : IAssetLoader<Material>
{
    /// <inheritdoc />
    public Material Load(string fullPath)
    {
        var data = MaterialJson.Read(File.ReadAllText(fullPath));
        var color = data.BaseColor;
        var emissive = data.Emissive;
        return new Material
        {
            BaseColor = color == null ? Color.White : new Color(color[0], color[1], color[2], color.Length == 4 ? color[3] : 1f),
            Texture = Resolve(data.Texture),
            Metallic = data.Metallic,
            Roughness = data.Roughness,
            MetallicRoughnessTexture = Resolve(data.MetallicRoughnessTexture),
            NormalTexture = Resolve(data.NormalTexture),
            NormalScale = data.NormalScale,
            Emissive = emissive == null ? new Color(0f, 0f, 0f) : new Color(emissive[0], emissive[1], emissive[2]),
            AlphaMode = MaterialJson.ParseAlphaMode(data.AlphaMode),
            AlphaCutoff = data.AlphaCutoff,
            Unlit = data.Unlit,
            DoubleSided = data.DoubleSided,
            CastShadows = data.CastShadows,
        };
    }

    private AssetHandle<Texture2D> Resolve(string? name) => string.IsNullOrWhiteSpace(name) ? default : assets.Load<Texture2D>(name);
}
