namespace Engine.Rendering.Tests;

public class RectTests
{
    [Fact]
    public void Empty_rect_maps_to_the_whole_texture()
    {
        Assert.Equal(Rect.UnitSquare, Rect.Empty.ToUv(64, 32));
    }

    [Fact]
    public void Inset_moves_every_edge_inwards()
    {
        Assert.Equal(new Rect(1.5f, 2.5f, 7f, 5f), new Rect(1f, 2f, 8f, 6f).Inset(0.5f));
    }

    [Fact]
    public void Pixel_rect_is_normalised_by_texture_size()
    {
        var uv = new Rect(16, 8, 32, 16).ToUv(64, 32);

        Assert.Equal(new Rect(0.25f, 0.25f, 0.5f, 0.5f), uv);
    }
}
