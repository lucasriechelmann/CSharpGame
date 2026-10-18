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

        // Texture coordinates, swapped per axis when flipped.
        float u0 = draw.UvRect.X;
        float u1 = draw.UvRect.X + draw.UvRect.Width;
        float v0 = draw.UvRect.Y;
        float v1 = draw.UvRect.Y + draw.UvRect.Height;
        if ((draw.Flip & SpriteFlip.Horizontal) != 0) (u0, u1) = (u1, u0);
        if ((draw.Flip & SpriteFlip.Vertical) != 0) (v0, v1) = (v1, v0);

        destination[0] = Corner(draw, left, top, cos, sin, u0, v0);      // top-left
        destination[1] = Corner(draw, right, top, cos, sin, u1, v0);     // top-right
        destination[2] = Corner(draw, right, bottom, cos, sin, u1, v1);  // bottom-right
        destination[3] = Corner(draw, left, bottom, cos, sin, u0, v1);   // bottom-left
    }

    private static SpriteVertex Corner(in SpriteDraw draw, float x, float y, float cos, float sin, float u, float v) => new()
    {
        Position = new Vector3(
            draw.Position.X + x * cos - y * sin,
            draw.Position.Y + x * sin + y * cos,
            draw.Position.Z),
        TexCoord = new Vector2(u, v),
        Color = draw.Color,
    };
}
