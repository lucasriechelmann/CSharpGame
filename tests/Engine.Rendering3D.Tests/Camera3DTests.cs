using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class Camera3DTests
{
    private static Camera3D Camera() => new()
    {
        Position = new Vector3(0f, 0f, 10f),
        ViewportSize = new Vector2(800f, 600f),
        Near = 1f,
        Far = 100f,
    };

    private static Vector3 Project(Matrix4x4 viewProjection, Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), viewProjection);
        return new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
    }

    [Fact]
    public void Default_camera_looks_down_negative_z()
    {
        var camera = Camera();

        Assert.Equal(-1f, camera.Forward.Z, 5);
        Assert.Equal(1f, camera.Up.Y, 5);
        Assert.Equal(1f, camera.Right.X, 5);
    }

    [Fact]
    public void Point_on_the_view_axis_projects_to_the_screen_centre()
    {
        var ndc = Project(Camera().GetViewProjection(), new Vector3(0f, 0f, 0f));

        Assert.Equal(0f, ndc.X, 5);
        Assert.Equal(0f, ndc.Y, 5);
    }

    [Fact]
    public void Near_and_far_planes_map_to_minus_one_and_one()
    {
        // OpenGL clip depth, not Direct3D's 0..1: this is why Projection3D exists.
        var viewProjection = Camera().GetViewProjection();

        Assert.Equal(-1f, Project(viewProjection, new Vector3(0f, 0f, 9f)).Z, 4); // 1 unit in front of the eye = near
        Assert.Equal(1f, Project(viewProjection, new Vector3(0f, 0f, -90f)).Z, 4); // 100 units in front = far
    }

    [Fact]
    public void Right_of_the_axis_is_positive_x_and_above_is_positive_y_on_screen()
    {
        var viewProjection = Camera().GetViewProjection();

        Assert.True(Project(viewProjection, new Vector3(1f, 0f, 0f)).X > 0f);
        Assert.True(Project(viewProjection, new Vector3(0f, 1f, 0f)).Y > 0f);
    }

    [Fact]
    public void Wider_aspect_ratio_shrinks_horizontal_scale()
    {
        var narrow = Camera();
        var wide = Camera();
        wide.ViewportSize = new Vector2(1600f, 600f);

        Assert.True(Project(wide.GetViewProjection(), Vector3.UnitX).X < Project(narrow.GetViewProjection(), Vector3.UnitX).X);
    }

    [Fact]
    public void LookAt_points_the_camera_at_the_target()
    {
        var camera = new Camera3D { Position = new Vector3(5f, 3f, 5f) };

        camera.LookAt(new Vector3(0f, 0f, 0f));

        var expected = Vector3.Normalize(new Vector3(-5f, -3f, -5f));
        Assert.Equal(expected.X, camera.Forward.X, 4);
        Assert.Equal(expected.Y, camera.Forward.Y, 4);
        Assert.Equal(expected.Z, camera.Forward.Z, 4);
        Assert.True(camera.Up.Y > 0f, "the camera must stay upright");
        Assert.Equal(0f, Vector3.Dot(camera.Right, camera.Forward), 4);
    }

    [Fact]
    public void LookAt_rejects_its_own_position_and_straight_up()
    {
        var camera = new Camera3D { Position = Vector3.Zero };

        Assert.Throws<ArgumentException>(() => camera.LookAt(Vector3.Zero));
        Assert.Throws<ArgumentException>(() => camera.LookAt(new Vector3(0f, 5f, 0f)));
    }

    [Fact]
    public void Screen_centre_ray_points_along_forward()
    {
        var camera = Camera();

        var ray = camera.ScreenPointToRay(new Vector2(400f, 300f));

        Assert.Equal(0f, ray.Direction.X, 4);
        Assert.Equal(0f, ray.Direction.Y, 4);
        Assert.Equal(-1f, ray.Direction.Z, 4);
        Assert.Equal(9f, ray.Origin.Z, 3); // starts on the near plane, 1 unit ahead of the eye
    }

    [Fact]
    public void Ray_through_a_projected_point_passes_through_that_point()
    {
        var camera = Camera();
        camera.LookAt(new Vector3(1f, 1f, 0f));
        var target = new Vector3(2f, -1f, -3f);

        Assert.True(camera.TryWorldToScreen(target, out var screen));
        var ray = camera.ScreenPointToRay(screen);

        float distance = Vector3.Dot(target - ray.Origin, ray.Direction);
        Assert.True((ray.GetPoint(distance) - target).Length() < 1e-2f);
    }

    [Fact]
    public void Point_behind_the_camera_has_no_screen_position()
    {
        Assert.False(Camera().TryWorldToScreen(new Vector3(0f, 0f, 20f), out _));
    }

    [Fact]
    public void Planes_and_field_of_view_are_validated()
    {
        var camera = Camera();

        Assert.Throws<ArgumentOutOfRangeException>(() => camera.FovY = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Near = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Near = 200f);
        Assert.Throws<ArgumentOutOfRangeException>(() => camera.Far = 0.5f);
    }
}
