using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

/// <summary>Textures made in code, for floors and test scenes that have no picture files: the pixels are computed once and uploaded like any texture.</summary>
public static class ProceduralTextures
{
    /// <summary>A two-tone grey checker that tiles and is mipmapped, so a far-away floor does not shimmer. Register it as an asset (<c>Assets.Add("checker", ...)</c>) for materials to name.</summary>
    /// <param name="gl">The GL context that will own the texture.</param>
    /// <param name="size">Width and height in pixels.</param>
    /// <param name="square">Side of one square in pixels; <paramref name="size"/> should be a multiple of twice this for the pattern to tile.</param>
    /// <param name="light">Brightness (0 to 255) of the light squares.</param>
    /// <param name="dark">Brightness (0 to 255) of the dark squares.</param>
    public static Texture2D Checker(GL gl, int size = 64, int square = 8, byte light = 230, byte dark = 120)
    {
        if (size <= 0 || square <= 0) throw new ArgumentOutOfRangeException(nameof(size), "The size and the squares must be positive.");

        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte value = ((x / square) + (y / square)) % 2 == 0 ? light : dark;
                int i = (y * size + x) * 4;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
        }

        var texture = Texture2D.FromPixels(gl, size, size, pixels, TextureFilter.Trilinear);
        texture.SetRepeat();
        return texture;
    }
}
