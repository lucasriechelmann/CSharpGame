using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>A material as read from a model file, before any GPU object exists: colour, shininess and the still-encoded colour image.</summary>
/// <param name="BaseColor">Colour multiplier (glTF baseColorFactor).</param>
/// <param name="Image">Encoded PNG or JPEG bytes of the base colour texture, or null for a plain colour.</param>
/// <param name="Shininess">Blinn-Phong exponent derived from the file's roughness.</param>
/// <param name="Specular">Highlight strength derived from the file's roughness and metalness.</param>
/// <param name="DoubleSided">Draw back faces too.</param>
public sealed record ModelMaterialData(Color BaseColor, byte[]? Image, float Shininess, float Specular, bool DoubleSided)
{
    /// <summary>What a primitive without a material gets (glTF's default: white, fully rough).</summary>
    public static ModelMaterialData Default { get; } = FromRoughness(Color.White, null, 1f, 1f, false);

    /// <summary>
    /// Maps glTF's metallic-roughness parameters onto Blinn-Phong, which has no such thing: a rough surface (roughness 1) gets a
    /// wide dull highlight, a smooth one a tight bright highlight. An approximation (shininess is about <c>2 / r^4 - 2</c>, the
    /// usual conversion from a microfacet distribution), good enough to make models look plausible without a PBR shader.
    /// </summary>
    public static ModelMaterialData FromRoughness(Color baseColor, byte[]? image, float roughness, float metallic, bool doubleSided)
    {
        float r = Math.Clamp(roughness, 0.05f, 1f);
        float shininess = Math.Clamp(2f / (r * r * r * r) - 2f, 4f, 256f);
        float specular = Math.Clamp(0.15f + 0.6f * (1f - r) + 0.25f * Math.Clamp(metallic, 0f, 1f) * (1f - r), 0f, 1f);
        return new ModelMaterialData(baseColor, image, shininess, specular, doubleSided);
    }
}
