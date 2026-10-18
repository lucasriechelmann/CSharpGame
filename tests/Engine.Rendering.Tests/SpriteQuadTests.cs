using System.Numerics;

namespace Engine.Rendering.Tests;

public class SpriteQuadTests
{
    private static SpriteVertex[] Build(SpriteDraw draw)
    {
        var vertices = new SpriteVertex[SpriteQuad.VertexCount];
        SpriteQuad.Write(draw, vertices);
        return vertices;
    }

    private static void AssertPosition(float x, float y, SpriteVertex vertex)
    {
        Assert.Equal(x, vertex.Position.X, 4);
        Assert.Equal(y, vertex.Position.Y, 4);
    }

    [Fact]
    public void Centre_pivot_places_corners_around_the_position()
    {
        var v = Build(SpriteDraw.At(new Vector3(10, 20, 5), new Vector2(4, 2)));

        AssertPosition(8, 21, v[0]);   // top-left
        AssertPosition(12, 21, v[1]);  // top-right
        AssertPosition(12, 19, v[2]);  // bottom-right
        AssertPosition(8, 19, v[3]);   // bottom-left
        Assert.All(v, vertex => Assert.Equal(5f, vertex.Position.Z));
    }

    [Fact]
    public void Top_left_pivot_extends_right_and_down()
    {
        var draw = SpriteDraw.At(new Vector3(10, 20, 0), new Vector2(4, 2));
        draw.Pivot = Vector2.Zero;
        var v = Build(draw);

        AssertPosition(10, 20, v[0]);
        AssertPosition(14, 18, v[2]);
    }

    [Fact]
    public void Bottom_middle_pivot_stands_the_sprite_on_its_position()
    {
        var draw = SpriteDraw.At(new Vector3(0, 0, 0), new Vector2(4, 6));
        draw.Pivot = new Vector2(0.5f, 1f);
        var v = Build(draw);

        AssertPosition(-2, 6, v[0]);
        AssertPosition(2, 0, v[2]);
    }

    [Fact]
    public void Rotation_turns_counter_clockwise_around_the_pivot()
    {
        var draw = SpriteDraw.At(new Vector3(10, 20, 0), new Vector2(4, 2));
        draw.Rotation = MathF.PI / 2f;
        var v = Build(draw);

        // Top-left was at offset (-2, +1); a quarter turn CCW sends it to (-1, -2).
        AssertPosition(9, 18, v[0]);
    }

    [Fact]
    public void Texture_coordinates_cover_the_uv_rect_with_top_left_origin()
    {
        var draw = SpriteDraw.At(Vector3.Zero, Vector2.One);
        draw.UvRect = new Rect(0.25f, 0.5f, 0.25f, 0.25f);
        var v = Build(draw);

        Assert.Equal(new Vector2(0.25f, 0.5f), v[0].TexCoord);
        Assert.Equal(new Vector2(0.5f, 0.5f), v[1].TexCoord);
        Assert.Equal(new Vector2(0.5f, 0.75f), v[2].TexCoord);
        Assert.Equal(new Vector2(0.25f, 0.75f), v[3].TexCoord);
    }

    [Fact]
    public void Horizontal_flip_swaps_left_and_right_coordinates()
    {
        var draw = SpriteDraw.At(Vector3.Zero, Vector2.One);
        draw.Flip = SpriteFlip.Horizontal;
        var v = Build(draw);

        Assert.Equal(1f, v[0].TexCoord.X);
        Assert.Equal(0f, v[1].TexCoord.X);
        Assert.Equal(0f, v[0].TexCoord.Y); // vertical untouched
    }

    [Fact]
    public void Vertical_flip_swaps_top_and_bottom_coordinates()
    {
        var draw = SpriteDraw.At(Vector3.Zero, Vector2.One);
        draw.Flip = SpriteFlip.Vertical;
        var v = Build(draw);

        Assert.Equal(1f, v[0].TexCoord.Y);
        Assert.Equal(0f, v[3].TexCoord.Y);
    }

    [Fact]
    public void Color_is_copied_to_every_vertex()
    {
        var draw = SpriteDraw.At(Vector3.Zero, Vector2.One);
        draw.Color = new Color(0.1f, 0.2f, 0.3f, 0.4f);

        Assert.All(Build(draw), vertex => Assert.Equal(new Color(0.1f, 0.2f, 0.3f, 0.4f), vertex.Color));
    }

    [Fact]
    public void Destination_that_is_too_small_throws()
    {
        var draw = SpriteDraw.At(Vector3.Zero, Vector2.One);
        Assert.Throws<ArgumentException>(() => SpriteQuad.Write(draw, new SpriteVertex[3]));
    }
}
