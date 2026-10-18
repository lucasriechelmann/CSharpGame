using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>What an <see cref="IPostEffect"/> needs to draw a pass: the GL API, the shared full-screen triangle and the window size.</summary>
public sealed class PostProcessContext
{
    /// <summary>Creates the context.</summary>
    public PostProcessContext(GL gl, FullscreenQuad quad, int screenWidth, int screenHeight)
    {
        Gl = gl;
        Quad = quad;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
    }

    /// <summary>The GL API.</summary>
    public GL Gl { get; }

    /// <summary>The shared full-screen triangle.</summary>
    public FullscreenQuad Quad { get; }

    /// <summary>Window width in pixels, the size of the final image.</summary>
    public int ScreenWidth { get; internal set; }

    /// <summary>Window height in pixels.</summary>
    public int ScreenHeight { get; internal set; }

    /// <summary>
    /// Directs drawing to <paramref name="destination"/>, or to the window when it is null, and switches off blending and depth
    /// testing, which a full-screen pass that replaces every pixel does not want.
    /// </summary>
    public void BindDestination(RenderTarget? destination)
    {
        if (destination != null) destination.Bind();
        else RenderTarget.BindScreen(Gl, ScreenWidth, ScreenHeight);

        Gl.Disable(EnableCap.Blend);
        Gl.Disable(EnableCap.DepthTest);
    }
}
