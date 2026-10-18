using System.Numerics;

namespace Engine.Rendering;

/// <summary>
/// Pure maths that turns a <see cref="SpriteDraw"/> into four vertices. Kept separate from
/// <see cref="SpriteBatch"/> so it can be unit-tested without a GPU.
/// </summary>
public static class SpriteQuad
{
    /// <summary>Vertices per quad. Order: top-left, top-right, bottom-right, bottom-left.</summary>
    public const int VertexCount = 4;

    /// <summary>Indices per quad (two triangles): 0,1,2 and 2,3,0 relative to the quad's first vertex.</summary>
    public const int IndexCount = 6;

    /// <summary>Writes the four corner vertices for <paramref name="draw"/>.</summary>
    /// <param name="draw">The sprite to build.</param>
    /// <param name="destination">Receives the vertices; must have room for <see cref="VertexCount"/>.</param>
    public static void Write(in SpriteDraw draw, Span<SpriteVertex> destination)
    {
        if (destination.Length < VertexCount)
            throw new ArgumentException($"Destination needs room for {VertexCount} vertices.", nameof(destination));

        // Local corner offsets from the pivot. World Y points up, so the top edge is above the pivot.
        float left = -draw.Pivot.X * draw.Size.X;
        float right = (1f - draw.Pivot.X) * draw.Size.X;
        float top = draw.Pivot.Y * draw.Size.Y;
        float bottom = -(1f - draw.Pivot.Y) * draw.Size.Y;

        float cos = MathF.Cos(draw.Rotation);
        float sin = MathF.Sin(draw.Rotation);

        // Each corner samples the tile at (a, b) in 0..1 across the region, a to the right and b downwards. A diagonal flip swaps the two
        // first, then the horizontal and vertical flips mirror the result (the order Tiled defines).
        destination[0] = Corner(draw, left, top, cos, sin, Sample(draw, 0f, 0f));      // top-left
        destination[1] = Corner(draw, right, top, cos, sin, Sample(draw, 1f, 0f));     // top-right
        destination[2] = Corner(draw, right, bottom, cos, sin, Sample(draw, 1f, 1f));  // bottom-right
        destination[3] = Corner(draw, left, bottom, cos, sin, Sample(draw, 0f, 1f));   // bottom-left
    }

    /// <summary>The texture coordinate a quad corner reads, for the corner's position (a across, b down) within the sprite after flipping.</summary>
    private static Vector2 Sample(in SpriteDraw draw, float a, float b)
    {
        if ((draw.Flip & SpriteFlip.Diagonal) != 0) (a, b) = (b, a);
        if ((draw.Flip & SpriteFlip.Horizontal) != 0) a = 1f - a;
        if ((draw.Flip & SpriteFlip.Vertical) != 0) b = 1f - b;
        return new Vector2(draw.UvRect.X + a * draw.UvRect.Width, draw.UvRect.Y + b * draw.UvRect.Height);
    }

    private static SpriteVertex Corner(in SpriteDraw draw, float x, float y, float cos, float sin, Vector2 texCoord) => new()
    {
        Position = new Vector3(
            draw.Position.X + x * cos - y * sin,
            draw.Position.Y + x * sin + y * cos,
            draw.Position.Z),
        TexCoord = texCoord,
        Color = draw.Color,
    };
}
