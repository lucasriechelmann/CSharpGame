namespace Engine.Rendering;

/// <summary>How a texture is sampled when drawn larger or smaller than its pixel size.</summary>
public enum TextureFilter
{
    /// <summary>Crisp, blocky pixels. The right choice for pixel art.</summary>
    Nearest,

    /// <summary>Smooth blending between pixels. Better for high-resolution art.</summary>
    Linear,

    /// <summary>
    /// Smooth blending plus mipmaps: pre-shrunk copies of the image, so a surface seen at a grazing angle or far away (a 3D floor)
    /// is averaged instead of shimmering. Costs a third more memory; pointless for 2D sprites drawn near 1:1.
    /// </summary>
    Trilinear,
}
