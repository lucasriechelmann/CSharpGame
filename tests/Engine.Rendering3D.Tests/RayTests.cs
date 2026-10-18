using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class RayTests
{
    private static readonly Ray AlongMinusZ = new(new Vector3(0f, 0f, 10f), -Vector3.UnitZ);

    [Fact]
    public void Sphere_in_the_path_is_hit_at_its_near_surface()
    {
        Assert.True(AlongMinusZ.IntersectsSphere(Vector3.Zero, 2f, out float distance));
        Assert.Equal(8f, distance, 4);
    }

    [Fact]
    public void Sphere_beside_the_path_is_missed()
    {
        Assert.False(AlongMinusZ.IntersectsSphere(new Vector3(5f, 0f, 0f), 2f, out _));
    }

    [Fact]
    public void Sphere_behind_the_origin_is_missed()
    {
        Assert.False(AlongMinusZ.IntersectsSphere(new Vector3(0f, 0f, 20f), 2f, out _));
    }

    [Fact]
    public void Origin_inside_a_sphere_hits_at_distance_zero()
    {
        Assert.True(AlongMinusZ.IntersectsSphere(new Vector3(0f, 0f, 10f), 2f, out float distance));
        Assert.Equal(0f, distance);
    }

    [Fact]
    public void Box_in_the_path_is_hit_at_its_front_face()
    {
        Assert.True(AlongMinusZ.IntersectsBox(new Vector3(-1f, -1f, -1f), new Vector3(1f, 1f, 1f), out float distance));
        Assert.Equal(9f, distance, 4);
    }

    [Fact]
    public void Box_beside_the_path_is_missed_even_though_the_ray_is_parallel_to_its_faces()
    {
        Assert.False(AlongMinusZ.IntersectsBox(new Vector3(3f, -1f, -1f), new Vector3(5f, 1f, 1f), out _));
    }

    [Fact]
    public void Box_behind_the_origin_is_missed()
    {
        Assert.False(AlongMinusZ.IntersectsBox(new Vector3(-1f, -1f, 20f), new Vector3(1f, 1f, 22f), out _));
    }

    [Fact]
    public void Diagonal_ray_hits_a_box_it_crosses()
    {
        var ray = new Ray(new Vector3(-5f, -5f, 0f), Vector3.Normalize(new Vector3(1f, 1f, 0f)));

        Assert.True(ray.IntersectsBox(new Vector3(-1f, -1f, -1f), new Vector3(1f, 1f, 1f), out float distance));
        Assert.Equal(MathF.Sqrt(32f), distance, 3); // enters the corner at (-1,-1): 4 units on each axis
    }
}
