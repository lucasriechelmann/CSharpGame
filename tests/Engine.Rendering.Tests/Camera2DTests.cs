using System.Numerics;

namespace Engine.Rendering.Tests;

public class Camera2DTests
{
    private static Camera2D NewCamera() => new() { ViewportSize = new Vector2(800, 600) };

    private static void AssertVector(float x, float y, Vector2 actual)
    {
        Assert.Equal(x, actual.X, 3);
        Assert.Equal(y, actual.Y, 3);
    }

    [Fact]
    public void Screen_centre_maps_to_camera_position()
    {
        var camera = NewCamera();
        camera.Position = new Vector2(100, 50);

        AssertVector(100, 50, camera.ScreenToWorld(new Vector2(400, 300)));
    }

    [Fact]
    public void Screen_top_left_is_up_and_left_of_the_camera()
    {
        // At zoom 1 one world unit is one pixel, and world Y points up while screen Y points down.
        AssertVector(-400, 300, NewCamera().ScreenToWorld(Vector2.Zero));
    }

    [Fact]
    public void Zoom_shrinks_the_visible_world()
    {
        var camera = NewCamera();
        camera.Zoom = 2f;

        AssertVector(-200, 150, camera.ScreenToWorld(Vector2.Zero));
    }

    [Fact]
    public void World_to_screen_is_the_inverse_of_screen_to_world()
    {
        var camera = NewCamera();
        camera.Position = new Vector2(-30, 75);
        camera.Zoom = 1.5f;
        camera.Rotation = 0.6f;

        var screen = new Vector2(123, 456);
        AssertVector(screen.X, screen.Y, camera.WorldToScreen(camera.ScreenToWorld(screen)));
    }

    [Fact]
    public void Rotating_the_camera_rotates_the_view_the_opposite_way()
    {
        var camera = NewCamera();
        camera.Rotation = MathF.PI / 2f; // camera "up" now points along world -X

        // The point to the right of screen centre is 90 degrees counter-clockwise from the camera's local +X.
        AssertVector(0, 100, camera.ScreenToWorld(new Vector2(500, 300)));
    }

    [Fact]
    public void Visible_bounds_cover_the_viewport_at_default_zoom()
    {
        var camera = NewCamera();
        camera.Position = new Vector2(100, 50);

        camera.GetVisibleBounds(out var min, out var max);

        AssertVector(-300, -250, min);
        AssertVector(500, 350, max);
    }

    [Fact]
    public void Zooming_in_shrinks_the_visible_bounds()
    {
        var camera = NewCamera();
        camera.Zoom = 2f;

        camera.GetVisibleBounds(out var min, out var max);

        AssertVector(-200, -150, min);
        AssertVector(200, 150, max);
    }

    [Fact]
    public void Rotation_swaps_the_visible_extents()
    {
        var camera = NewCamera();
        camera.Rotation = MathF.PI / 2f; // an 800x600 view turned a quarter turn covers 600 wide by 800 tall

        camera.GetVisibleBounds(out var min, out var max);

        AssertVector(-300, -400, min);
        AssertVector(300, 400, max);
    }

    [Fact]
    public void Non_positive_zoom_is_rejected()
    {
        var camera = NewCamera();
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Zoom = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Zoom = -1f);
    }

    [Fact]
    public void World_origin_is_clip_centre_by_default()
    {
        var clip = Vector3.Transform(Vector3.Zero, NewCamera().GetViewProjection());

        Assert.Equal(0f, clip.X, 4);
        Assert.Equal(0f, clip.Y, 4);
    }
}
