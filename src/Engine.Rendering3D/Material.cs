using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// How a surface looks: a base colour (multiplied with the texture, if any) and how shiny it is. A shared asset, so many
/// <see cref="MeshRenderer"/>s can point at one and a change shows on all of them. Plain data with no GPU objects: the texture is
/// referenced by handle and resolved when drawing, so it survives a texture hot-reload.
/// </summary>
public sealed class Material
{
    /// <summary>Surface colour. Multiplied with <see cref="Texture"/> when there is one; alpha is ignored (no transparency yet).</summary>
    public Color BaseColor { get; set; } = Color.White;

    /// <summary>Optional colour texture; the default (invalid) handle means a plain colour.</summary>
    public AssetHandle<Texture2D> Texture { get; set; }

    /// <summary>Sharpness of the specular highlight (Blinn-Phong exponent): 8 is rough, 128 is polished.</summary>
    public float Shininess { get; set; } = 32f;

    /// <summary>Strength of the specular highlight, 0 (matte) to 1.</summary>
    public float Specular { get; set; } = 0.3f;

    /// <summary>When true the surface ignores lights and shows its colour as-is (skies, glowing things, debugging).</summary>
    public bool Unlit { get; set; }

    /// <summary>When true back faces are drawn too (thin sheets such as leaves); off by default so the hidden side of a solid costs nothing.</summary>
    public bool DoubleSided { get; set; }
}
