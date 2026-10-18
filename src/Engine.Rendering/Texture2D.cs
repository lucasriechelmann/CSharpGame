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

        int glFilter = filter == TextureFilter.Nearest ? (int)GLEnum.Nearest : (int)GLEnum.Linear;
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, glFilter);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, glFilter);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

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

    /// <summary>A 1x1 opaque white texture. Draw it with a tint <see cref="Color"/> to get solid-colour quads.</summary>
    public static Texture2D CreateWhite(GL gl) => FromPixels(gl, 1, 1, [255, 255, 255, 255]);

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
