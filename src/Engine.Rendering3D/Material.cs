using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// How a surface looks, as a metallic-roughness (PBR) material: a base colour (multiplied with the texture, if any), how metallic and how
/// rough it is, optional maps for those and for surface detail, and how its alpha is used. A shared asset, so many
/// <see cref="MeshRenderer"/>s can point at one and a change shows on all of them. Plain data with no GPU objects: textures are
/// referenced by handle and resolved when drawing, so they survive a texture hot-reload.
/// </summary>
public sealed class Material
{
    /// <summary>Surface colour (sRGB). Multiplied with <see cref="Texture"/> when there is one; alpha matters per <see cref="AlphaMode"/>.</summary>
    public Color BaseColor { get; set; } = Color.White;

    /// <summary>Optional colour texture (sRGB); the default (invalid) handle means a plain colour.</summary>
    public AssetHandle<Texture2D> Texture { get; set; }

    /// <summary>
    /// A colour texture the material owns itself, such as an image embedded in a model file. Takes precedence over <see cref="Texture"/>; not
    /// saved in material files (a loader sets it) and disposed by whoever created it (the <see cref="Model"/>), not by the asset manager.
    /// </summary>
    public Texture2D? OwnedTexture { get; set; }

    /// <summary>How much of a metal the surface is, 0 (plastic, wood, stone) to 1 (bare metal). Metals have no diffuse colour: their base colour tints the reflection.</summary>
    public float Metallic { get; set; }

    /// <summary>How rough the surface is, 0 (mirror) to 1 (chalk). Sets how wide and dim highlights are.</summary>
    public float Roughness { get; set; } = 0.6f;

    /// <summary>Optional map of roughness (green channel) and metallic (blue channel), multiplied with <see cref="Roughness"/> and <see cref="Metallic"/> (glTF's layout). Linear data, not sRGB.</summary>
    public AssetHandle<Texture2D> MetallicRoughnessTexture { get; set; }

    /// <summary>The same map owned by the material (embedded in a model file); takes precedence.</summary>
    public Texture2D? OwnedMetallicRoughnessTexture { get; set; }

    /// <summary>Optional tangent-space normal map: it bends the surface normal per pixel to fake small bumps and grooves. Linear data (green = up the image, the glTF and OpenGL convention).</summary>
    public AssetHandle<Texture2D> NormalTexture { get; set; }

    /// <summary>The normal map owned by the material (embedded in a model file); takes precedence.</summary>
    public Texture2D? OwnedNormalTexture { get; set; }

    /// <summary>How strongly the normal map bends the normal: 1 as authored, 0 flat, above 1 exaggerated.</summary>
    public float NormalScale { get; set; } = 1f;

    /// <summary>Light the surface gives off by itself (sRGB), added after lighting and unaffected by it; black for none.</summary>
    public Color Emissive { get; set; } = new(0f, 0f, 0f);

    /// <summary>How alpha is used; see <see cref="Rendering3D.AlphaMode"/>.</summary>
    public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;

    /// <summary>With <see cref="Rendering3D.AlphaMode.Mask"/>: pixels with less alpha than this are discarded.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>When true the surface ignores lights and shows its colour (plus emission) as-is (skies, glowing things, debugging).</summary>
    public bool Unlit { get; set; }

    /// <summary>When true back faces are drawn too (thin sheets such as leaves); off by default so the hidden side of a solid costs nothing.</summary>
    public bool DoubleSided { get; set; }

    /// <summary>When false the surface casts no shadow (a glowing sprite-like quad, a skybox). Blended surfaces never cast shadows.</summary>
    public bool CastShadows { get; set; } = true;

    /// <summary>True when the surface must be drawn after the opaque ones, sorted back to front.</summary>
    public bool IsBlended => AlphaMode == AlphaMode.Blend;
}
