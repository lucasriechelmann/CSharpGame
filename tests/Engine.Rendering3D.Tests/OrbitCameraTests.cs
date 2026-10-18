using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class OrbitCameraTests
{
    private static void AssertNear(Vector3 expected, Vector3 actual, int digits = 4)
    {
        Assert.Equal(expected.X, actual.X, digits);
        Assert.Equal(expected.Y, actual.Y, digits);
        Assert.Equal(expected.Z, actual.Z, digits);
    }

    [Fact]
    public void The_eye_sits_behind_the_target_along_the_view_direction()
    {
        var orbit = new OrbitCamera { Target = new Vector3(1, 2, 3), Distance = 10f };

        // Zero angles look down -Z, so the eye is 10 units towards +Z.
        AssertNear(new Vector3(1, 2, 13), orbit.Position);
    }

    [Fact]
    public void Orbiting_keeps_the_distance_to_the_target()
    {
        var orbit = new OrbitCamera { Target = new Vector3(1, 2, 3), Distance = 7f };

        orbit.Orbit(1.2f, 0.4f);

        Assert.Equal(7f, Vector3.Distance(orbit.Position, orbit.Target), 4);
    }

    [Fact]
    public void Pitch_is_clamped_short_of_straight_up()
    {
        var orbit = new OrbitCamera();

        orbit.Orbit(0f, 10f);

        Assert.Equal(OrbitCamera.MaxPitch, orbit.Pitch);
    }

    [Fact]
    public void Positive_pitch_puts_the_eye_below_the_target()
    {
        var orbit = new OrbitCamera { Pitch = 0.5f };

        Assert.True(orbit.Position.Y < orbit.Target.Y);
    }

    [Fact]
    public void Zoom_in_shrinks_the_distance_and_out_grows_it()
    {
        var orbit = new OrbitCamera { Distance = 10f };

        orbit.Zoom(1f);
        Assert.True(orbit.Distance < 10f);

        orbit.Zoom(-3f);
        Assert.True(orbit.Distance > 10f);
    }

    [Fact]
    public void Zoom_stops_at_the_limits()
    {
        var orbit = new OrbitCamera { Distance = 1f };

        orbit.Zoom(1000f);
        Assert.Equal(OrbitCamera.MinDistance, orbit.Distance);

        orbit.Zoom(-1000f);
        Assert.Equal(OrbitCamera.MaxDistance, orbit.Distance);
    }

    [Fact]
    public void Panning_moves_the_target_in_the_screen_plane()
    {
        var orbit = new OrbitCamera { Target = Vector3.Zero, Distance = 10f };
        float fov = MathF.PI / 2f; // tan(fov/2) = 1, so 720 px span 20 world units at distance 10
        float perPixel = 20f / 720f;

        orbit.Pan(new Vector2(36f, 0f), fov, 720f); // dragging right slides the world right, so the camera goes left

        AssertNear(new Vector3(-36f * perPixel, 0f, 0f), orbit.Target);
    }

    [Fact]
    public void Dragging_down_pans_the_camera_up()
    {
        var orbit = new OrbitCamera { Distance = 10f };

        orbit.Pan(new Vector2(0f, 72f), MathF.PI / 2f, 720f);

        Assert.True(orbit.Target.Y > 0f);
    }

    [Fact]
    public void Focus_centres_the_target_and_backs_off_until_the_sphere_fits()
    {
        var orbit = new OrbitCamera();
        float fov = MathF.PI / 3f;

        orbit.FocusOn(new Vector3(5, 1, 0), 2f, fov);

        Assert.Equal(new Vector3(5, 1, 0), orbit.Target);
        // The sphere's silhouette must fit the vertical field of view: distance * sin(fov/2) >= radius.
        Assert.True(orbit.Distance * MathF.Sin(fov / 2f) >= 2f);
    }

    [Fact]
    public void A_tiny_object_still_gets_a_sensible_distance()
    {
        var orbit = new OrbitCamera();

        orbit.FocusOn(Vector3.Zero, 0f, MathF.PI / 3f);

        Assert.True(orbit.Distance > OrbitCamera.MinDistance);
    }

    [Fact]
    public void Starting_from_a_camera_reproduces_its_view()
    {
        var camera = new Camera3D { Position = new Vector3(0f, 3.5f, 9f) };
        camera.LookAt(new Vector3(0f, 1f, 0f));

        var orbit = OrbitCamera.From(camera, 6f);
        var copy = new Camera3D();
        orbit.ApplyTo(copy);

        AssertNear(camera.Position, copy.Position);
        AssertNear(camera.Forward, copy.Forward);
    }

    [Fact]
    public void Fly_camera_can_take_over_where_the_orbit_camera_left_off()
    {
        var orbit = new OrbitCamera { Target = new Vector3(2, 1, -3), Yaw = 0.7f, Pitch = -0.3f, Distance = 5f };
        var camera = new Camera3D();
        orbit.ApplyTo(camera);

        var fly = new FlyCamera();
        fly.CopyFrom(camera);
        var again = new Camera3D();
        fly.ApplyTo(again);

        AssertNear(camera.Position, again.Position);
        AssertNear(camera.Forward, again.Forward);
    }
}
