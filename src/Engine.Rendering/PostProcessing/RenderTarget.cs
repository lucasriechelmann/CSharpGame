using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// An off-screen image you can draw into: an OpenGL framebuffer object whose colour attachment is a <see cref="Texture2D"/>.
/// Render the scene into one, then read <see cref="Texture"/> in a later pass; that is the basis of post-processing and lighting.
/// A <see cref="RenderTargetFormat"/> chooses 8-bit or HDR colour, depth/stencil buffers, multisampling and the sampling filter.
/// Owns the framebuffer(s), the texture and the extra renderbuffers.
/// </summary>
/// <remarks>
/// A multisampled target draws into renderbuffers (a texture cannot be sampled while it holds several samples per pixel) and
/// <see cref="Resolve"/> averages them into <see cref="Texture"/>; call it before reading the texture.
/// </remarks>
public sealed class RenderTarget : IDisposable
{
    private readonly GL _gl;
    private uint _framebuffer;
    private uint _multisampleFramebuffer;
    private uint _multisampleColor;
    private uint _depthBuffer;

    /// <summary>Creates a target of the given size. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the target.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="withDepth">Attach a 24-bit depth buffer so 3D geometry can be depth-tested while drawing into the target. Costs memory, so only the scene target asks for it.</param>
    /// <exception cref="InvalidOperationException">The driver reports the framebuffer as incomplete.</exception>
    public RenderTarget(GL gl, int width, int height, bool withDepth = false)
        : this(gl, width, height, new RenderTargetFormat(Depth: withDepth))
    {
    }

    /// <summary>Creates a target with a full format description.</summary>
    /// <param name="gl">The GL context that will own the target.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="format">Colour precision, extra buffers, multisampling and filter.</param>
    /// <exception cref="ArgumentOutOfRangeException">The format is invalid.</exception>
    /// <exception cref="InvalidOperationException">The driver reports the framebuffer as incomplete.</exception>
    public RenderTarget(GL gl, int width, int height, RenderTargetFormat format)
    {
        format.Validate();
        _gl = gl;
        Format = format;
        Texture = null!;
        Create(width, height);
    }

    /// <summary>The target's format.</summary>
    public RenderTargetFormat Format { get; }

    /// <summary>The colour image. For a multisampled target it holds the last <see cref="Resolve"/>d result.</summary>
    public Texture2D Texture { get; private set; }

    /// <summary>Width in pixels.</summary>
    public int Width => Texture.Width;

    /// <summary>Height in pixels.</summary>
    public int Height => Texture.Height;

    /// <summary>Makes this target the draw destination and sets the viewport to cover it. A multisampled target binds its multisampled buffers.</summary>
    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Format.IsMultisampled ? _multisampleFramebuffer : _framebuffer);
        _gl.Viewport(0, 0, (uint)Width, (uint)Height);
    }

    /// <summary>Makes the window's own framebuffer the destination again.</summary>
    /// <param name="gl">The GL context.</param>
    /// <param name="width">Window width in pixels.</param>
    /// <param name="height">Window height in pixels.</param>
    public static void BindScreen(GL gl, int width, int height)
    {
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    /// <summary>For a multisampled target, averages the samples into <see cref="Texture"/>; does nothing for a plain target. Call it after drawing and before sampling the texture.</summary>
    public void Resolve()
    {
        if (!Format.IsMultisampled) return;

        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _multisampleFramebuffer);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _framebuffer);
        // Colour only: nothing reads the multisampled depth afterwards. Source and destination have the same size, so Nearest is exact.
        _gl.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    /// <summary>Changes the size, recreating the buffers (contents are lost). Does nothing when the size is unchanged.</summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    public void Resize(int width, int height)
    {
        if (width == Width && height == Height) return;

        Destroy();
        Create(width, height);
    }

    /// <summary>Frees the framebuffers, renderbuffers and texture.</summary>
    public void Dispose() => Destroy();

    private void Create(int width, int height)
    {
        Texture = Texture2D.CreateEmpty(_gl, width, height, Format.Filter, Format.Hdr);
        _framebuffer = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture.Handle, 0);

        bool needsDepth = Format.Depth || Format.Stencil;
        if (!Format.IsMultisampled && needsDepth) AttachDepth(width, height, samples: 0);
        CheckComplete(width, height);

        if (Format.IsMultisampled)
        {
            // The drawing happens here; the texture above only receives the resolved result.
            _multisampleFramebuffer = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _multisampleFramebuffer);
            _multisampleColor = _gl.GenRenderbuffer();
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _multisampleColor);
            _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)Format.Samples,
                Format.Hdr ? InternalFormat.Rgba16f : InternalFormat.Rgba8, (uint)width, (uint)height);
            _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _multisampleColor);
            if (needsDepth) AttachDepth(width, height, Format.Samples);
            CheckComplete(width, height);
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void AttachDepth(int width, int height, int samples)
    {
        // A renderbuffer rather than a texture: nothing samples the depth later, and renderbuffers are what drivers optimise for depth.
        _depthBuffer = _gl.GenRenderbuffer();
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
        var storage = Format.Stencil ? InternalFormat.Depth24Stencil8 : InternalFormat.DepthComponent24;
        if (samples > 1) _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)samples, storage, (uint)width, (uint)height);
        else _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, storage, (uint)width, (uint)height);

        var attachment = Format.Stencil ? FramebufferAttachment.DepthStencilAttachment : FramebufferAttachment.DepthAttachment;
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, _depthBuffer);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    private void CheckComplete(int width, int height)
    {
        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status == GLEnum.FramebufferComplete) return;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Destroy();
        throw new InvalidOperationException($"Render target {width}x{height} ({Format}) is incomplete: {status}.");
    }

    private void Destroy()
    {
        if (_framebuffer != 0) _gl.DeleteFramebuffer(_framebuffer);
        if (_multisampleFramebuffer != 0) _gl.DeleteFramebuffer(_multisampleFramebuffer);
        if (_multisampleColor != 0) _gl.DeleteRenderbuffer(_multisampleColor);
        if (_depthBuffer != 0) _gl.DeleteRenderbuffer(_depthBuffer);
        _framebuffer = _multisampleFramebuffer = _multisampleColor = _depthBuffer = 0;

        Texture.Dispose();
    }
}
