using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Platformer;

/// <summary>Procedurally generated textures so the demo needs no art assets.</summary>
public static class TestTextures
{
    /// <summary>
    /// An 8x8 two-tone checkerboard with a bright top-left corner pixel, so flips, rotation and
    /// texture-coordinate orientation are visible at a glance.
    /// </summary>
    public static Texture2D CreateChecker(GL gl)
    {
        const int size = 8;
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool dark = (x + y) % 2 == 0;
                byte shade = dark ? (byte)150 : (byte)255;
                int i = (y * size + x) * 4;
                pixels[i + 0] = shade;
                pixels[i + 1] = shade;
                pixels[i + 2] = shade;
                pixels[i + 3] = 255;
            }
        }

        // Mark the top-left pixel (row 0 = top of the image) red.
        pixels[0] = 255;
        pixels[1] = 0;
        pixels[2] = 0;

        return Texture2D.FromPixels(gl, size, size, pixels, TextureFilter.Nearest);
    }
}
