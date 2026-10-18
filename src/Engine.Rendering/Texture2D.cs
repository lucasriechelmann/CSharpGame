using Silk.NET.OpenGL;
using StbImageSharp;

namespace Engine.Rendering;

/// <summary>
/// A 2D RGBA texture living on the GPU. Owns the GL texture object, so dispose it when no longer needed.
/// Row 0 of the pixel data is the top of the image, matching how sprites map texture coordinates.
/// </summary>
public sealed class Texture2D : IDisposable
{
    private readonly GL _gl;

    private Texture2D(GL gl, uint handle, int width, int height)
    {
        _gl = gl;
        Handle = handle;
        Width = width;
        Height = height;
    }

    /// <summary>The OpenGL texture object name.</summary>
    public uint Handle { get; }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Uploads raw 8-bit RGBA pixels (4 bytes per pixel, rows top to bottom).</summary>
    /// <param name="gl">The GL context that will own the texture.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="rgba">Exactly <c>width * height * 4</c> bytes.</param>
    /// <param name="filter">How the texture is sampled when scaled.</param>
    public static Texture2D FromPixels(GL gl, int width, int height, ReadOnlySpan<byte> rgba, TextureFilter filter = TextureFilter.Nearest)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions must be positive.");
        if (rgba.Length != width * height * 4)
            throw new ArgumentException($"Expected {width * height * 4} bytes for {width}x{height} RGBA, got {rgba.Length}.", nameof(rgba));

        uint handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, handle);

        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, rgba);

        int magFilter = filter == TextureFilter.Nearest ? (int)GLEnum.Nearest : (int)GLEnum.Linear;
        int minFilter = filter == TextureFilter.Trilinear ? (int)GLEnum.LinearMipmapLinear : magFilter;
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, minFilter);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, magFilter);
        // Clamping suits sprites (no bleeding across atlas cells); 3D surfaces want tiling, see Texture2D.SetRepeat.
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        if (filter == TextureFilter.Trilinear) gl.GenerateMipmap(TextureTarget.Texture2D);

        return new Texture2D(gl, handle, width, height);
    }

    /// <summary>Loads a PNG/JPG/BMP/TGA file from disk.</summary>
    /// <param name="gl">The GL context that will own the texture.</param>
    /// <param name="path">Path to the image file.</param>
    /// <param name="filter">How the texture is sampled when scaled.</param>
    public static Texture2D FromFile(GL gl, string path, TextureFilter filter = TextureFilter.Nearest)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return FromPixels(gl, image.Width, image.Height, image.Data, filter);
    }

    /// <summary>
    /// Allocates an uninitialised RGBA texture to render into (the colour attachment of a <see cref="RenderTarget"/>). Linear
    /// filtering so a target drawn smaller or larger than its size still looks smooth.
    /// </summary>
    /// <param name="gl">The GL context that will own the texture.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    public static unsafe Texture2D CreateEmpty(GL gl, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), $"Texture dimensions must be positive, got {width}x{height}.");

        uint handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, handle);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        return new Texture2D(gl, handle, width, height);
    }

    /// <summary>A 1x1 opaque white texture. Draw it with a tint <see cref="Color"/> to get solid-colour quads.</summary>
    public static Texture2D CreateWhite(GL gl) => FromPixels(gl, 1, 1, [255, 255, 255, 255]);

    /// <summary>
    /// Makes the texture tile (wrap around) instead of clamping at its edges, so UV coordinates beyond 0..1 repeat the image. 3D
    /// surfaces such as floors and walls need it; sprite sheets do not (clamping stops neighbouring cells bleeding in).
    /// </summary>
    public void SetRepeat()
    {
        _gl.BindTexture(TextureTarget.Texture2D, Handle);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
    }

    /// <summary>Binds the texture to a texture unit for the next draw call.</summary>
    /// <param name="unit">Texture unit index (0 for the first).</param>
    public void Bind(int unit = 0)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + unit);
        _gl.BindTexture(TextureTarget.Texture2D, Handle);
    }

    /// <summary>Deletes the GL texture.</summary>
    public void Dispose() => _gl.DeleteTexture(Handle);
}
