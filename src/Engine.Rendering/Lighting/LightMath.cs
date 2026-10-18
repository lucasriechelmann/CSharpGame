using System.Numerics;

namespace Engine.Rendering;

/// <summary>Pure light maths kept apart from GL so it can be unit-tested: the brightness falloff, the gradient texture built from it, and culling.</summary>
public static class LightMath
{
    /// <summary>
    /// Brightness at a distance from a light's centre, as a fraction of the radius: 1 at the centre, 0 at and beyond the edge,
    /// smooth in between ((1 - d²)², which has no visible ring at the edge, unlike a straight linear ramp).
    /// </summary>
    /// <param name="distance">Distance from the centre divided by the radius.</param>
    public static float Falloff(float distance)
    {
        if (distance >= 1f) return 0f;

        float d = Math.Max(distance, 0f);
        float t = 1f - d * d;
        return t * t;
    }

    /// <summary>
    /// Builds the RGBA pixels of a square light sprite: white everywhere, with alpha set by <see cref="Falloff"/> from the
    /// centre. Drawn additively and tinted, it becomes a coloured pool of light.
    /// </summary>
    /// <param name="size">Width and height in pixels.</param>
    /// <returns><c>size * size * 4</c> bytes, rows top to bottom.</returns>
    public static byte[] BuildGradientPixels(int size)
    {
        if (size < 2) throw new ArgumentOutOfRangeException(nameof(size), size, "A gradient needs at least 2x2 pixels.");

        var pixels = new byte[size * size * 4];
        float centre = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = MathF.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) / centre;
                int i = (y * size + x) * 4;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = 255;
                pixels[i + 3] = (byte)MathF.Round(Falloff(distance) * 255f);
            }
        }

        return pixels;
    }

    /// <summary>True when a light's circle can touch the visible box, so lights entirely off-screen are not drawn.</summary>
    /// <param name="visibleMin">Bottom-left of the visible world box.</param>
    /// <param name="visibleMax">Top-right of the visible world box.</param>
    /// <param name="centre">Light position.</param>
    /// <param name="radius">Light radius.</param>
    public static bool Overlaps(Vector2 visibleMin, Vector2 visibleMax, Vector2 centre, float radius) =>
        centre.X + radius >= visibleMin.X && centre.X - radius <= visibleMax.X
        && centre.Y + radius >= visibleMin.Y && centre.Y - radius <= visibleMax.Y;
}
