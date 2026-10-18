using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class PointLightSelectorTests
{
    [Fact]
    public void Lights_that_cannot_reach_the_mesh_are_skipped()
    {
        Vector3[] positions = [new(100f, 0f, 0f), new(2f, 0f, 0f)];
        float[] radii = [5f, 5f];
        Span<int> chosen = stackalloc int[4];

        int count = PointLightSelector.Select(positions, radii, Vector3.Zero, 1f, chosen);

        Assert.Equal(1, count);
        Assert.Equal(1, chosen[0]);
    }

    [Fact]
    public void Lights_are_ordered_by_how_deep_the_mesh_sits_in_their_falloff()
    {
        // Light 0 is far inside its radius (score 0.2), light 1 barely reaches (0.9), light 2 is in between (0.5).
        Vector3[] positions = [new(2f, 0f, 0f), new(10f, 0f, 0f), new(6f, 0f, 0f)];
        float[] radii = [5f, 10f, 10f];
        Span<int> chosen = stackalloc int[4];

        int count = PointLightSelector.Select(positions, radii, Vector3.Zero, 1f, chosen);

        Assert.Equal(3, count);
        Assert.Equal([0, 2, 1], chosen[..3].ToArray());
    }

    [Fact]
    public void With_more_lights_than_slots_the_best_ones_win()
    {
        Vector3[] positions = [new(9f, 0f, 0f), new(1f, 0f, 0f), new(5f, 0f, 0f), new(3f, 0f, 0f)];
        float[] radii = [20f, 20f, 20f, 20f];
        Span<int> chosen = stackalloc int[2];

        int count = PointLightSelector.Select(positions, radii, Vector3.Zero, 0.5f, chosen);

        Assert.Equal(2, count);
        Assert.Equal([1, 3], chosen.ToArray()); // the two nearest
    }

    [Fact]
    public void A_light_with_no_radius_is_ignored()
    {
        Span<int> chosen = stackalloc int[2];

        Assert.Equal(0, PointLightSelector.Select([Vector3.Zero], [0f], Vector3.Zero, 1f, chosen));
    }

    [Fact]
    public void No_lights_selects_nothing()
    {
        Span<int> chosen = stackalloc int[2];

        Assert.Equal(0, PointLightSelector.Select([], [], Vector3.Zero, 1f, chosen));
    }
}
