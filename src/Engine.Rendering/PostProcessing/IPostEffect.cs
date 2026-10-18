namespace Engine.Rendering;

/// <summary>
/// One full-screen image effect (colour grading, vignette, bloom, lighting). Effects form a chain: each reads the image the
/// previous one produced and writes the next, and the last writes to the window.
/// </summary>
public interface IPostEffect : IDisposable
{
    /// <summary>Name shown in tools.</summary>
    string Name { get; }

    /// <summary>Disabled effects are skipped, and when none is enabled the scene is drawn straight to the window at no extra cost.</summary>
    bool Enabled { get; set; }

    /// <summary>Reads <paramref name="source"/> and writes the processed image to <paramref name="destination"/>.</summary>
    /// <param name="context">Draw helpers and screen size.</param>
    /// <param name="source">The image to process.</param>
    /// <param name="destination">Where to write, or null for the window.</param>
    void Apply(PostProcessContext context, Texture2D source, RenderTarget? destination);
}
