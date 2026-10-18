namespace Engine.Rendering;

/// <summary>What a <see cref="RenderTarget"/> is made of: how precise its colours are, which extra buffers it has, and how it is smoothed.</summary>
/// <param name="Hdr">16-bit float colour instead of 8-bit, so values above 1 survive until a tone-map.</param>
/// <param name="Depth">A 24-bit depth buffer, needed to depth-test 3D geometry while drawing into the target.</param>
/// <param name="Stencil">A depth buffer with 8 stencil bits (implies depth); used to mask areas, for example light shadows.</param>
/// <param name="Samples">Multisampling: 1 is off, 2/4/8 draw into a multisampled buffer that <see cref="RenderTarget.Resolve"/> averages into the texture, smoothing polygon edges.</param>
/// <param name="Filter">How the texture is sampled when it is drawn scaled; <see cref="TextureFilter.Nearest"/> keeps hard pixels.</param>
public readonly record struct RenderTargetFormat(
    bool Hdr = false,
    bool Depth = false,
    bool Stencil = false,
    int Samples = 1,
    TextureFilter Filter = TextureFilter.Linear)
{
    /// <summary>8-bit colour, no extra buffers, linear filtering: what post effects use for intermediate images.</summary>
    public static RenderTargetFormat Default => new(false, false, false, 1, TextureFilter.Linear);

    /// <summary>Whether this format draws into a multisampled buffer.</summary>
    public bool IsMultisampled => Samples > 1;

    /// <summary>Checks the numbers make sense.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The sample count is not 1, 2, 4, 8 or 16.</exception>
    public void Validate()
    {
        if (Samples is not (1 or 2 or 4 or 8 or 16))
            throw new ArgumentOutOfRangeException(nameof(Samples), Samples, "Samples must be 1, 2, 4, 8 or 16.");
    }
}
