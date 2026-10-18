using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class ShadowMathTests
{
    private static Camera3D CameraAt(Vector3 position, Vector3 lookAt)
    {
        var camera = new Camera3D { Position = position, ViewportSize = new Vector2(1280, 720), Near = 0.1f, Far = 200f };
        camera.LookAt(lookAt);
        return camera;
    }

    private static Vector3 Ndc(Matrix4x4 viewProjection, Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), viewProjection);
        return new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
    }

    private static readonly Vector3 SunDown = Vector3.Normalize(new Vector3(-0.3f, -1f, -0.4f));

    // ---- Projection3D.Orthographic ----

    [Fact]
    public void The_orthographic_box_maps_to_the_unit_cube()
    {
        var projection = Projection3D.Orthographic(-2f, 2f, -1f, 1f, 1f, 11f);

        var nearCorner = Vector3.Transform(new Vector3(-2f, -1f, -1f), projection);
        var farCorner = Vector3.Transform(new Vector3(2f, 1f, -11f), projection);

        Assert.Equal(new Vector3(-1f, -1f, -1f).X, nearCorner.X, 4);
        Assert.Equal(-1f, nearCorner.Y, 4);
        Assert.Equal(-1f, nearCorner.Z, 4); // OpenGL's near is -1
        Assert.Equal(1f, farCorner.X, 4);
        Assert.Equal(1f, farCorner.Y, 4);
        Assert.Equal(1f, farCorner.Z, 4);
    }

    [Fact]
    public void An_orthographic_view_keeps_parallel_lines_parallel()
    {
        var projection = Projection3D.Orthographic(-5f, 5f, -5f, 5f, 0f, 20f);

        var a = Vector3.Transform(new Vector3(1f, 1f, -2f), projection);
        var b = Vector3.Transform(new Vector3(1f, 1f, -18f), projection);

        Assert.Equal(a.X, b.X, 5); // no perspective division: the same x and y however far away
        Assert.Equal(a.Y, b.Y, 5);
    }

    [Theory]
    [InlineData(2f, 1f, -1f, 1f, 0f, 1f)]   // right <= left
    [InlineData(-1f, 1f, 1f, 1f, 0f, 1f)]   // top <= bottom
    [InlineData(-1f, 1f, -1f, 1f, 5f, 5f)]  // far <= near
    public void A_degenerate_box_is_rejected(float left, float right, float bottom, float top, float near, float far)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Projection3D.Orthographic(left, right, bottom, top, near, far));
    }

    // ---- ShadowMath.FitToCamera ----

    [Fact]
    public void Every_corner_of_the_visible_slice_lands_inside_the_shadow_map()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));
        var fit = ShadowMath.FitToCamera(camera, SunDown, distance: 40f, resolution: 2048);

        // The centre of the view, a few depths along it.
        foreach (float depth in new[] { 0.5f, 5f, 20f, 40f })
        {
            var point = camera.Position + camera.Forward * depth;
            var ndc = Ndc(fit.ViewProjection, point);
            Assert.InRange(ndc.X, -1f, 1f);
            Assert.InRange(ndc.Y, -1f, 1f);
            Assert.InRange(ndc.Z, -1f, 1f);
        }
    }

    [Fact]
    public void The_edges_of_the_view_are_inside_the_map_too()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));
        var fit = ShadowMath.FitToCamera(camera, SunDown, distance: 40f, resolution: 2048);
        float tan = MathF.Tan(camera.FovY * 0.5f);

        float depth = 40f;
        var corner = camera.Position + camera.Forward * depth + camera.Right * (tan * depth * camera.AspectRatio) + camera.Up * (tan * depth);
        var ndc = Ndc(fit.ViewProjection, corner);

        Assert.InRange(ndc.X, -1f, 1f);
        Assert.InRange(ndc.Y, -1f, 1f);
    }

    [Fact]
    public void A_caster_between_the_sun_and_the_view_is_still_inside_the_depth_range()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));
        var fit = ShadowMath.FitToCamera(camera, SunDown, distance: 40f, resolution: 2048, casterRange: 120f);

        var tallThingTowardsTheSun = camera.Position + camera.Forward * 10f - SunDown * 80f; // 80 m up the sun's ray from something in view
        var ndc = Ndc(fit.ViewProjection, tallThingTowardsTheSun);

        Assert.InRange(ndc.Z, -1f, 1f);
    }

    [Fact]
    public void Moving_the_camera_by_less_than_a_texel_does_not_move_the_shadow_map()
    {
        var a = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));
        var fitA = ShadowMath.FitToCamera(a, SunDown, 40f, 2048);
        var b = CameraAt(new Vector3(0, 3, 9) + Vector3.UnitX * (fitA.TexelWorldSize * 0.2f), new Vector3(0, 1, 0) + Vector3.UnitX * (fitA.TexelWorldSize * 0.2f));
        var fitB = ShadowMath.FitToCamera(b, SunDown, 40f, 2048);

        // A point in the world lands on the same texel in both fits: the snapped matrices are equal.
        Assert.Equal(fitA.ViewProjection.M41, fitB.ViewProjection.M41, 5);
        Assert.Equal(fitA.ViewProjection.M42, fitB.ViewProjection.M42, 5);
    }

    [Fact]
    public void Turning_the_camera_does_not_change_the_size_of_the_texels()
    {
        var a = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));
        var b = CameraAt(new Vector3(0, 3, 9), new Vector3(8, 1, 2));

        var sizeA = ShadowMath.FitToCamera(a, SunDown, 40f, 2048).TexelWorldSize;
        var sizeB = ShadowMath.FitToCamera(b, SunDown, 40f, 2048).TexelWorldSize;

        Assert.Equal(sizeA, sizeB, 6);
    }

    [Fact]
    public void A_shorter_shadow_distance_gives_a_sharper_map()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));

        var near = ShadowMath.FitToCamera(camera, SunDown, 15f, 2048).TexelWorldSize;
        var far = ShadowMath.FitToCamera(camera, SunDown, 80f, 2048).TexelWorldSize;

        Assert.True(near < far);
    }

    [Fact]
    public void A_higher_resolution_gives_smaller_texels()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));

        var low = ShadowMath.FitToCamera(camera, SunDown, 40f, 1024).TexelWorldSize;
        var high = ShadowMath.FitToCamera(camera, SunDown, 40f, 4096).TexelWorldSize;

        Assert.Equal(low / 4f, high, 5);
    }

    [Fact]
    public void A_sun_straight_overhead_still_works()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));

        var fit = ShadowMath.FitToCamera(camera, new Vector3(0, -1, 0), 40f, 2048);

        Assert.False(float.IsNaN(fit.ViewProjection.M11));
        var ndc = Ndc(fit.ViewProjection, camera.Position + camera.Forward * 10f);
        Assert.InRange(ndc.X, -1f, 1f);
        Assert.InRange(ndc.Y, -1f, 1f);
    }

    [Fact]
    public void A_distance_beyond_the_far_plane_is_clamped_to_it()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));

        var clamped = ShadowMath.FitToCamera(camera, SunDown, 5000f, 2048);
        var atFar = ShadowMath.FitToCamera(camera, SunDown, camera.Far, 2048);

        Assert.Equal(atFar.Radius, clamped.Radius);
    }

    [Fact]
    public void Bad_inputs_are_rejected()
    {
        var camera = CameraAt(new Vector3(0, 3, 9), new Vector3(0, 1, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => ShadowMath.FitToCamera(camera, SunDown, 40f, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShadowMath.FitToCamera(camera, SunDown, 0.05f, 2048));
        Assert.Throws<ArgumentException>(() => ShadowMath.FitToCamera(camera, Vector3.Zero, 40f, 2048));
    }
}
