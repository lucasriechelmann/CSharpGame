using System.Numerics;
using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Runtime.Views;

/// <summary>
/// A second picture of the same world, drawn into its own texture with its own camera and its own post-processing chain: a minimap, a rear-view
/// mirror, a security-camera screen. This is how a camera gets effects of its own: the main view keeps <see cref="Game.PostProcess"/>, a view owns a
/// separate <see cref="PostProcessChain"/> (<see cref="Chain"/>), so a minimap can be washed out while the main picture glows.
/// </summary>
/// <remarks>
/// Why it borrows the render systems' camera instead of owning one: every render system is built with the camera it draws through, so the view
/// does not need new systems. Just before it draws, <c>apply</c> points that shared camera at what the view should see (position, zoom, viewport
/// size) and just after, <c>restore</c> puts it back. The view then runs the same <see cref="Game.RenderSystems"/> into its own target.
/// </remarks>
public sealed class RenderView : IDisposable
{
    private readonly GL _gl;
    private readonly Action _apply;
    private readonly Action _restore;

    /// <summary>Creates a view with a texture of the given size. Needs a current GL context (create it in <c>Load</c>).</summary>
    /// <param name="gl">The GL context.</param>
    /// <param name="width">Width of the view's picture in pixels.</param>
    /// <param name="height">Height of the view's picture in pixels.</param>
    /// <param name="apply">Points the shared camera at what this view shows, including its viewport size (<paramref name="width"/> by <paramref name="height"/>).</param>
    /// <param name="restore">Puts the shared camera back the way <paramref name="apply"/> found it.</param>
    public RenderView(GL gl, int width, int height, Action apply, Action restore)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(restore);
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "A view needs a positive width.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "A view needs a positive height.");

        _gl = gl;
        _apply = apply;
        _restore = restore;
        Width = width;
        Height = height;
        Chain = new PostProcessChain(gl, width, height);
        Chain.UseOffscreenOutput(true);
    }

    /// <summary>The view's own effect chain. Add effects to it exactly as to <see cref="Game.PostProcess"/>; they affect only this picture.</summary>
    public PostProcessChain Chain { get; }

    /// <summary>Width of the picture in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Height of the picture in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>False skips drawing the view (and its cost); the texture keeps the last picture.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Colour the view is cleared to before the world is drawn over it.</summary>
    public Vector4 ClearColor { get; set; } = new(0.07f, 0.07f, 0.09f, 1f);

    /// <summary>The finished picture, to show in the HUD (rows run bottom to top, like any GL texture). Null only before the first resize.</summary>
    public Texture2D? Texture => Chain.OutputTexture;

    /// <summary>Changes the picture's size. The <c>apply</c> callback should read <see cref="Width"/> and <see cref="Height"/> again.</summary>
    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;

        Width = width;
        Height = height;
        Chain.Resize(width, height);
    }

    /// <summary>Draws the view: camera on, clear, run <paramref name="drawWorld"/> (the game's render systems), effects, camera back.</summary>
    internal void Render(Action drawWorld)
    {
        _apply();
        try
        {
            Chain.BeginScene();
            _gl.ClearColor(ClearColor.X, ClearColor.Y, ClearColor.Z, ClearColor.W);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            drawWorld();
            Chain.EndScene();
        }
        finally
        {
            _restore();
        }
    }

    /// <summary>Frees the chain and its targets.</summary>
    public void Dispose() => Chain.Dispose();
}
