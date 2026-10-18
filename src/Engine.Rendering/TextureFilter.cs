namespace Engine.Rendering;

/// <summary>How a texture is sampled when drawn larger or smaller than its pixel size.</summary>
public enum TextureFilter
{
    /// <summary>Crisp, blocky pixels. The right choice for pixel art.</summary>
    Nearest,

    /// <summary>Smooth blending between pixels. Better for high-resolution art.</summary>
    Linear,
}
