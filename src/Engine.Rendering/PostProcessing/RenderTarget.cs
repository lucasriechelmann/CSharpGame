using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// An off-screen image you can draw into: an OpenGL framebuffer object whose colour attachment is a <see cref="Texture2D"/>.
/// Render the scene into one, then read <see cref="Texture"/> in a later pass; that is the basis of post-processing and lighting.
/// Optionally has a depth buffer too (needed to draw 3D). Owns the framebuffer, its texture and the depth renderbuffer.
/// </summary>
public sealed class RenderTarget : IDisposable
{
    private readonly GL _gl;
    private readonly bool _withDepth;
    private uint _framebuffer;
    private uint _depthBuffer;

    /// <summary>Creates a target of the given size. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the target.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="withDepth">Attach a 24-bit depth buffer so 3D geometry can be depth-tested while drawing into the target. Costs memory, so only the scene target asks for it.</param>
    /// <exception cref="InvalidOperationException">The driver reports the framebuffer as incomplete.</exception>
    public RenderTarget(GL gl, int width, int height, bool withDepth = false)
    {
        _gl = gl;
        _withDepth = withDepth;
        Texture = null!;
        Create(width, height);
    }

    /// <summary>The colour image. Bind it to a texture unit to sample what was drawn. Replaced by <see cref="Resize"/>, so do not cache it.</summary>
    public Texture2D Texture { get; private set; }

    /// <summary>Width in pixels.</summary>
    public int Width => Texture.Width;

    /// <summary>Height in pixels.</summary>
    public int Height => Texture.Height;

    /// <summary>Makes this target the destination of subsequent draws and sets the viewport to cover it.</summary>
    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        _gl.Viewport(0, 0, (uint)Width, (uint)Height);
    }

    /// <summary>Directs drawing back to the window (framebuffer 0) with a viewport of the given size.</summary>
    public static void BindScreen(GL gl, int width, int height)
    {
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    /// <summary>Reallocates the target at a new size (contents are lost). Does nothing when the size is unchanged.</summary>
    public void Resize(int width, int height)
    {
        if (width == Width && height == Height) return;

        Destroy();
        Create(width, height);
    }

    /// <summary>Deletes the framebuffer and its texture.</summary>
    public void Dispose() => Destroy();

    private void Create(int width, int height)
    {
        Texture = Texture2D.CreateEmpty(_gl, width, height);
        _framebuffer = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture.Handle, 0);

        if (_withDepth)
        {
            // A renderbuffer rather than a texture: nothing samples the depth later, and renderbuffers are what drivers optimise for depth.
            _depthBuffer = _gl.GenRenderbuffer();
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)width, (uint)height);
            _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthBuffer);
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        if (status != GLEnum.FramebufferComplete)
        {
            Destroy();
            throw new InvalidOperationException($"Render target {width}x{height} is incomplete: {status}.");
        }
    }

    private void Destroy()
    {
        _gl.DeleteFramebuffer(_framebuffer);
        if (_depthBuffer != 0)
        {
            _gl.DeleteRenderbuffer(_depthBuffer);
            _depthBuffer = 0;
        }

        Texture.Dispose();
    }
}
