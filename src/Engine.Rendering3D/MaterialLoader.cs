using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// Asset loader for <c>.material.json</c> files. The texture it names is loaded through the manager (so a texture loader must be
/// registered); the material keeps only the handle.
/// </summary>
/// <param name="assets">The manager that owns the textures.</param>
public sealed class MaterialLoader(AssetManager assets) : IAssetLoader<Material>
{
    /// <inheritdoc />
    public Material Load(string fullPath)
    {
        var data = MaterialJson.Read(File.ReadAllText(fullPath));
        var color = data.BaseColor;
        return new Material
        {
            BaseColor = color == null ? Color.White : new Color(color[0], color[1], color[2], color.Length == 4 ? color[3] : 1f),
            Texture = string.IsNullOrWhiteSpace(data.Texture) ? default : assets.Load<Texture2D>(data.Texture),
            Shininess = data.Shininess,
            Specular = data.Specular,
            Unlit = data.Unlit,
            DoubleSided = data.DoubleSided,
        };
    }
}
