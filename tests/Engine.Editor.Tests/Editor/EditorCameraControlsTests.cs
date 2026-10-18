using System.Numerics;
using Engine.Rendering;
using Engine.Editor;

namespace Engine.Editor.Tests.Editor;

public class EditorCameraControlsTests
{
    private static Camera2D CreateCamera(float zoom = 1f) =>
        new() { ViewportSize = new Vector2(800f, 600f), Zoom = zoom, Position = new Vector2(10f, 20f) };

    [Fact]
    public void Panning_keeps_the_world_under_the_mouse()
    {
        var camera = CreateCamera(zoom: 2f);
        var mouse = new Vector2(300f, 200f);
        var worldUnderMouse = camera.ScreenToWorld(mouse);

        EditorCameraControls.Pan(camera, new Vector2(40f, -30f)); // the mouse moves to (340, 170)

        Assert.Equal(worldUnderMouse.X, camera.ScreenToWorld(mouse + new Vector2(40f, -30f)).X, 3);
        Assert.Equal(worldUnderMouse.Y, camera.ScreenToWorld(mouse + new Vector2(40f, -30f)).Y, 3);
    }

    [Fact]
    public void Dragging_right_moves_the_view_left()
    {
        var camera = CreateCamera();

        EditorCameraControls.Pan(camera, new Vector2(50f, 0f));

        Assert.Equal(-40f, camera.Position.X, 3);
        Assert.Equal(20f, camera.Position.Y, 3);
    }

    [Fact]
    public void Dragging_down_moves_the_view_up_in_the_world()
    {
        var camera = CreateCamera();

        EditorCameraControls.Pan(camera, new Vector2(0f, 50f)); // screen Y grows downward

        Assert.Equal(70f, camera.Position.Y, 3);
    }

    [Fact]
    public void A_pan_covers_less_world_when_zoomed_in()
    {
        var camera = CreateCamera(zoom: 4f);

        EditorCameraControls.Pan(camera, new Vector2(40f, 0f));

        Assert.Equal(0f, camera.Position.X, 3); // 10 - 40 / 4
    }

    [Fact]
    public void Zooming_in_scales_by_the_step_per_tick()
    {
        var camera = CreateCamera();

        EditorCameraControls.ZoomAt(camera, new Vector2(400f, 300f), 2f);

        Assert.Equal(1.21f, camera.Zoom, 3);
    }

    [Fact]
    public void Zooming_keeps_the_world_point_under_the_cursor_fixed()
    {
        var camera = CreateCamera();
        var cursor = new Vector2(650f, 120f);
        var before = camera.ScreenToWorld(cursor);

        EditorCameraControls.ZoomAt(camera, cursor, 5f);

        var after = camera.ScreenToWorld(cursor);
        Assert.Equal(before.X, after.X, 2);
        Assert.Equal(before.Y, after.Y, 2);
    }

    [Fact]
    public void Zoom_is_clamped_at_both_ends()
    {
        var camera = CreateCamera();

        EditorCameraControls.ZoomAt(camera, Vector2.Zero, 500f);
        Assert.Equal(EditorCameraControls.MaxZoom, camera.Zoom);

        EditorCameraControls.ZoomAt(camera, Vector2.Zero, -5000f);
        Assert.Equal(EditorCameraControls.MinZoom, camera.Zoom);
    }

    [Fact]
    public void Zero_scroll_changes_nothing()
    {
        var camera = CreateCamera();

        EditorCameraControls.ZoomAt(camera, new Vector2(1f, 1f), 0f);

        Assert.Equal(1f, camera.Zoom);
        Assert.Equal(new Vector2(10f, 20f), camera.Position);
    }
}
