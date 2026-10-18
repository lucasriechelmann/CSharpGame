using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

/// <summary>
/// A depth-only framebuffer the scene is drawn into from the sun's point of view. The depth texture is set up for hardware comparison
/// (<c>sampler2DShadow</c>): the GPU compares a reference depth against the stored one and returns 0 or 1, filtered bilinearly, so one
/// lookup already softens an edge. Outside the map the border counts as fully lit.
/// </summary>
internal sealed class ShadowMap : IDisposable
{
    private readonly GL _gl;
    private readonly uint _framebuffer;
    private readonly uint _texture;
    private int _previousFramebuffer;
    private readonly int[] _previousViewport = new int[4];

    /// <summary>Creates the map. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that owns the texture and framebuffer.</param>
    /// <param name="resolution">Side of the square map in texels (2048 is a good default; 4096 for crisper shadows).</param>
    /// <exception cref="InvalidOperationException">The framebuffer is incomplete on this GPU.</exception>
    public unsafe ShadowMap(GL gl, int resolution)
    {
        _gl = gl;
        Resolution = resolution;

        _texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, _texture);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, (uint)resolution, (uint)resolution, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        float* border = stackalloc float[] { 1f, 1f, 1f, 1f }; // beyond the map is "nothing in the way"
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, border);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);

        _framebuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _texture, 0);
        gl.DrawBuffer(DrawBufferMode.None); // depth only: there is no colour attachment to write
        gl.ReadBuffer(ReadBufferMode.None);
        var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        if (status != GLEnum.FramebufferComplete)
            throw new InvalidOperationException($"The shadow map framebuffer is incomplete ({status}); this GPU may not support a {resolution}x{resolution} depth texture.");
    }

    /// <summary>Side of the square map in texels.</summary>
    public int Resolution { get; }

    /// <summary>Starts rendering into the map: remembers the current framebuffer and viewport, binds the map and clears its depth.</summary>
    public void Begin()
    {
        _gl.GetInteger(GetPName.DrawFramebufferBinding, out _previousFramebuffer);
        _gl.GetInteger(GetPName.Viewport, _previousViewport);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        _gl.Viewport(0, 0, (uint)Resolution, (uint)Resolution);
        _gl.DepthMask(true);
        _gl.Clear(ClearBufferMask.DepthBufferBit);
    }

    /// <summary>Goes back to the framebuffer and viewport that were current at <see cref="Begin"/> (the window, or the post-processing scene target).</summary>
    public void End()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)_previousFramebuffer);
        _gl.Viewport(_previousViewport[0], _previousViewport[1], (uint)_previousViewport[2], (uint)_previousViewport[3]);
    }

    /// <summary>Binds the depth texture to a texture unit for the main shader to sample.</summary>
    /// <param name="unit">The texture unit.</param>
    public void Bind(int unit)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + unit);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
    }

    /// <summary>Deletes the framebuffer and the texture.</summary>
    public void Dispose()
    {
        _gl.DeleteFramebuffer(_framebuffer);
        _gl.DeleteTexture(_texture);
    }
}
