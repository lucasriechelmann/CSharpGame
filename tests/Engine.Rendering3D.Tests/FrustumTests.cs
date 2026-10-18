using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class FrustumTests
{
    private static Frustum LookingDownMinusZ()
    {
        var camera = new Camera3D
        {
            Position = Vector3.Zero,
            ViewportSize = new Vector2(100f, 100f), // square, so the 60 degree field of view is the same both ways
            Near = 1f,
            Far = 50f,
        };
        return camera.GetFrustum();
    }

    [Fact]
    public void Sphere_in_front_of_the_camera_is_visible()
    {
        Assert.True(LookingDownMinusZ().IntersectsSphere(new Vector3(0f, 0f, -10f), 1f));
    }

    [Fact]
    public void Sphere_behind_the_camera_is_culled()
    {
        Assert.False(LookingDownMinusZ().IntersectsSphere(new Vector3(0f, 0f, 10f), 1f));
    }

    [Fact]
    public void Sphere_beyond_the_far_plane_is_culled()
    {
        Assert.False(LookingDownMinusZ().IntersectsSphere(new Vector3(0f, 0f, -60f), 1f));
    }

    [Fact]
    public void Sphere_in_front_of_the_near_plane_is_culled()
    {
        Assert.False(LookingDownMinusZ().IntersectsSphere(new Vector3(0f, 0f, -0.2f), 0.1f));
    }

    [Fact]
    public void Sphere_far_to_the_side_is_culled_but_a_big_one_reaching_in_is_not()
    {
        var frustum = LookingDownMinusZ();

        // At depth 10 the half-width of the view is 10 * tan(30 degrees) = 5.77.
        Assert.False(frustum.IntersectsSphere(new Vector3(20f, 0f, -10f), 1f));
        Assert.True(frustum.IntersectsSphere(new Vector3(8f, 0f, -10f), 3f));
    }

    [Fact]
    public void Sphere_above_and_below_the_view_is_culled()
    {
        var frustum = LookingDownMinusZ();

        Assert.False(frustum.IntersectsSphere(new Vector3(0f, 30f, -10f), 1f));
        Assert.False(frustum.IntersectsSphere(new Vector3(0f, -30f, -10f), 1f));
    }
}
