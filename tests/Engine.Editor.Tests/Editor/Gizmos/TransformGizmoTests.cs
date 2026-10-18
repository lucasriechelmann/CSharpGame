using System.Numerics;
using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Editor.Gizmos;

namespace Engine.Editor.Tests.Editor.Gizmos;

public class TransformGizmoTests
{
    private static readonly Vector2 Centre = new(640f, 360f); // the middle of the default 1280x720 view: world (0, 0) in 2D

    private readonly Camera2D _camera2D = new();
    private readonly IGizmoView _view2D;
    private readonly GizmoFrame _origin = new(Vector3.Zero, Quaternion.Identity);

    public TransformGizmoTests() => _view2D = new OrthographicGizmoView(_camera2D);

    private static IGizmoView View3D(Vector3? position = null, Vector3? lookAt = null)
    {
        var camera = new Camera3D { Position = position ?? new Vector3(0, 0, 10), ViewportSize = new Vector2(1280, 720) };
        if (lookAt is { } target) camera.LookAt(target);
        return new PerspectiveGizmoView(camera);
    }

    // ---- hit testing, 2D ----

    [Fact]
    public void The_centre_square_is_the_free_handle()
    {
        var gizmo = new TransformGizmo();

        Assert.Equal(GizmoHandle.Free, gizmo.HitTest(_view2D, Centre + new Vector2(3f, -4f), _origin));
    }

    [Fact]
    public void The_right_pointing_arrow_is_the_x_handle_and_the_upward_one_is_y()
    {
        var gizmo = new TransformGizmo();

        Assert.Equal(GizmoHandle.AxisX, gizmo.HitTest(_view2D, Centre + new Vector2(40f, 3f), _origin));
        Assert.Equal(GizmoHandle.AxisY, gizmo.HitTest(_view2D, Centre + new Vector2(-2f, -40f), _origin)); // screen Y grows downward
    }

    [Theory]
    [InlineData(40f, 20f)]   // too far from the X arrow's line
    [InlineData(-40f, 0f)]   // left of the origin: no arrow there
    [InlineData(0f, 40f)]    // below the origin: the Y arrow points up
    [InlineData(TransformGizmo.ArrowLength + 20f, 0f)] // past the arrow's tip (and its head)
    [InlineData(200f, 200f)]
    public void Elsewhere_is_no_handle(float dx, float dy)
    {
        var gizmo = new TransformGizmo();

        Assert.Equal(GizmoHandle.None, gizmo.HitTest(_view2D, Centre + new Vector2(dx, dy), _origin));
    }

    [Fact]
    public void In_2D_there_is_no_z_arrow_and_only_a_z_ring()
    {
        var gizmo = new TransformGizmo();
        Assert.Equal(GizmoHandle.None, gizmo.HitTest(_view2D, Centre + new Vector2(0f, 0f) + new Vector2(20f, 20f), _origin));

        gizmo.Mode = GizmoMode.Rotate;
        Assert.Equal(GizmoHandle.AxisZ, gizmo.HitTest(_view2D, Centre + new Vector2(TransformGizmo.RingRadius, 0f), _origin));
        Assert.Equal(GizmoHandle.None, gizmo.HitTest(_view2D, Centre + new Vector2(3f, 3f), _origin)); // inside the ring there is nothing, not even a free handle
    }

    [Fact]
    public void The_handles_keep_their_pixel_size_at_any_zoom()
    {
        _camera2D.Zoom = 4f;
        var gizmo = new TransformGizmo();

        Assert.Equal(GizmoHandle.AxisX, gizmo.HitTest(_view2D, Centre + new Vector2(60f, 0f), _origin));
    }

    [Fact]
    public void A_scale_gizmo_uses_the_entitys_axes_even_in_world_space()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale, Space = GizmoSpace.World };
        var turned = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);

        var frame = gizmo.FrameFor(Vector3.Zero, turned);

        Assert.Equal(turned, frame.Orientation);
        Assert.Equal(Quaternion.Identity, new TransformGizmo().FrameFor(Vector3.Zero, turned).Orientation); // translate in world space ignores it
    }

    [Fact]
    public void Local_space_translate_follows_the_entitys_orientation()
    {
        var gizmo = new TransformGizmo { Space = GizmoSpace.Local };
        var turned = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);

        var frame = gizmo.FrameFor(Vector3.Zero, turned);

        Assert.Equal(0f, frame.Axis(0).X, 5);
        Assert.Equal(1f, frame.Axis(0).Y, 5);
    }

    [Fact]
    public void Where_two_handles_overlap_the_nearer_one_wins()
    {
        var gizmo = new TransformGizmo();
        var perspective = View3D();
        var frame = new GizmoFrame(Vector3.Zero, Quaternion.Identity);
        gizmo.HitTest(perspective, Centre, frame); // does not throw at the centre

        Assert.Equal(GizmoHandle.AxisX, gizmo.HitTest(perspective, Centre + new Vector2(50f, 2f), frame));
        Assert.Equal(GizmoHandle.AxisY, gizmo.HitTest(perspective, Centre + new Vector2(2f, -50f), frame));
    }

    [Fact]
    public void A_gizmo_behind_the_camera_cannot_be_grabbed()
    {
        var gizmo = new TransformGizmo();
        var view = View3D();

        Assert.Equal(GizmoHandle.None, gizmo.HitTest(view, Centre, new GizmoFrame(new Vector3(0, 0, 50), Quaternion.Identity)));
    }

    // ---- translate ----

    [Fact]
    public void An_x_drag_moves_along_x_only_and_ignores_vertical_mouse_movement()
    {
        var gizmo = new TransformGizmo();
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(100f, 77f));

        Assert.Equal(60f, delta.Translation.X, 3);
        Assert.Equal(0f, delta.Translation.Y, 3);
    }

    [Fact]
    public void A_y_drag_moves_up_when_the_mouse_moves_up()
    {
        var gizmo = new TransformGizmo();
        gizmo.Begin(_view2D, GizmoHandle.AxisY, Centre + new Vector2(0f, -40f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(50f, -100f));

        Assert.Equal(60f, delta.Translation.Y, 3);
        Assert.Equal(0f, delta.Translation.X, 3);
    }

    [Fact]
    public void The_entity_does_not_jump_on_the_first_frame_of_a_drag()
    {
        var gizmo = new TransformGizmo();
        var grab = Centre + new Vector2(45f, 0f);
        gizmo.Begin(_view2D, GizmoHandle.AxisX, grab, _origin);

        Assert.Equal(Vector3.Zero, gizmo.Drag(_view2D, grab).Translation);
    }

    [Fact]
    public void A_free_drag_follows_the_mouse_in_the_screen_plane()
    {
        var gizmo = new TransformGizmo();
        gizmo.Begin(_view2D, GizmoHandle.Free, Centre, _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(30f, -10f));

        Assert.Equal(30f, delta.Translation.X, 3);
        Assert.Equal(10f, delta.Translation.Y, 3); // up the screen is +Y
        Assert.Equal(0f, delta.Translation.Z, 3);
    }

    [Fact]
    public void Zoom_divides_the_distance_a_pixel_moves()
    {
        _camera2D.Zoom = 2f;
        var gizmo = new TransformGizmo();
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);

        Assert.Equal(25f, gizmo.Drag(_view2D, Centre + new Vector2(90f, 0f)).Translation.X, 3);
    }

    [Fact]
    public void Snapping_rounds_the_moved_distance_to_the_grid()
    {
        var gizmo = new TransformGizmo();
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);

        Assert.Equal(56f, gizmo.Drag(_view2D, Centre + new Vector2(93f, 0f), snap: 8f).Translation.X, 3); // moved 53 -> 56
    }

    [Fact]
    public void A_3D_arrow_drag_moves_by_pixels_times_the_world_size_of_a_pixel()
    {
        var view = View3D();
        var gizmo = new TransformGizmo();
        gizmo.Begin(view, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);

        var delta = gizmo.Drag(view, Centre + new Vector2(90f, 0f));

        float perPixel = 2f * 10f * MathF.Tan(MathF.PI / 6f) / 720f;
        Assert.Equal(50f * perPixel, delta.Translation.X, 2);
        Assert.Equal(0f, delta.Translation.Y, 3);
    }

    [Fact]
    public void A_3D_z_arrow_moves_along_z()
    {
        // Seen from the side the Z axis runs across the screen.
        var view = View3D(position: new Vector3(10, 0, 0), lookAt: Vector3.Zero);
        var gizmo = new TransformGizmo();
        var handle = gizmo.HitTest(view, Centre + new Vector2(-40f, 0f), _origin);

        Assert.Equal(GizmoHandle.AxisZ, handle); // from +X looking at the origin, +Z points to the left of the screen
        gizmo.Begin(view, handle, Centre + new Vector2(-40f, 0f), _origin);
        var delta = gizmo.Drag(view, Centre + new Vector2(-90f, 0f));

        Assert.True(delta.Translation.Z > 0.5f);
        Assert.Equal(0f, delta.Translation.X, 3);
    }

    [Fact]
    public void An_axis_pointing_straight_at_the_camera_gives_no_movement_instead_of_a_wild_one()
    {
        var view = View3D(); // looking down -Z, so the Z axis points at the camera
        var gizmo = new TransformGizmo();
        gizmo.Begin(view, GizmoHandle.AxisZ, Centre + new Vector2(0f, 0f), _origin);

        Assert.Equal(GizmoDelta.None, gizmo.Drag(view, Centre + new Vector2(200f, 0f)));
    }

    // ---- rotate ----

    [Fact]
    public void Dragging_round_the_ring_a_quarter_turn_counter_clockwise_turns_a_quarter_about_z()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Rotate };
        gizmo.Begin(_view2D, GizmoHandle.AxisZ, Centre + new Vector2(64f, 0f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(0f, -64f)); // up the screen is counter-clockwise from the right

        Assert.Equal(MathF.PI / 2f, AngleAboutZ(delta.Rotation), 3);
    }

    [Fact]
    public void Dragging_clockwise_turns_the_other_way()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Rotate };
        gizmo.Begin(_view2D, GizmoHandle.AxisZ, Centre + new Vector2(64f, 0f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(0f, 64f));

        Assert.Equal(-MathF.PI / 2f, AngleAboutZ(delta.Rotation), 3);
    }

    [Fact]
    public void Rotation_snaps_to_the_step_in_radians()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Rotate };
        float step = MathF.PI / 12f; // 15 degrees
        gizmo.Begin(_view2D, GizmoHandle.AxisZ, Centre + new Vector2(64f, 0f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(58f, -30f), snap: step); // about 27 degrees

        Assert.Equal(2f * step, AngleAboutZ(delta.Rotation), 3); // 30 degrees
    }

    [Fact]
    public void A_3D_ring_hit_test_finds_the_ring_and_a_drag_turns_about_its_axis()
    {
        var view = View3D();
        var gizmo = new TransformGizmo { Mode = GizmoMode.Rotate };
        // Seen from +Z the Z ring is a circle of 64 px around the centre; the X and Y rings are edge-on lines through it.
        // Grab it at 45 degrees, away from the points where those lines cross it.
        float r = 64f * MathF.Sqrt(0.5f);
        var grab = Centre + new Vector2(r, -r);
        Assert.Equal(GizmoHandle.AxisZ, gizmo.HitTest(view, grab, _origin));

        gizmo.Begin(view, GizmoHandle.AxisZ, grab, _origin);
        var delta = gizmo.Drag(view, Centre + new Vector2(-r, -r)); // 45 -> 135 degrees: a quarter turn counter-clockwise

        Assert.Equal(MathF.PI / 2f, AngleAboutZ(delta.Rotation), 2);
    }

    [Fact]
    public void Rotation_has_no_free_handle()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Rotate };

        Assert.Throws<ArgumentException>(() => gizmo.Begin(_view2D, GizmoHandle.Free, Centre, _origin));
    }

    // ---- scale ----

    [Fact]
    public void Dragging_an_axis_stalk_out_twice_as_far_doubles_that_axis()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(80f, 0f));

        Assert.Equal(new Vector3(2f, 1f, 1f), delta.Scale);
    }

    [Fact]
    public void The_centre_cube_scales_uniformly_and_leaves_z_alone_in_2D()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };
        gizmo.Begin(_view2D, GizmoHandle.Free, Centre + new Vector2(20f, 0f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(0f, -40f));

        Assert.Equal(new Vector3(2f, 2f, 1f), delta.Scale);
    }

    [Fact]
    public void A_3D_uniform_scale_scales_every_axis()
    {
        var view = View3D();
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };
        gizmo.Begin(view, GizmoHandle.Free, Centre + new Vector2(20f, 0f), _origin);

        Assert.Equal(new Vector3(3f), gizmo.Drag(view, Centre + new Vector2(60f, 0f)).Scale);
    }

    [Fact]
    public void Dragging_through_the_centre_cannot_flip_or_collapse_the_scale()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);

        var delta = gizmo.Drag(_view2D, Centre + new Vector2(-100f, 0f));

        Assert.Equal(TransformGizmo.MinScaleFactor, delta.Scale.X);
    }

    [Fact]
    public void Scale_snaps_to_the_step()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);

        Assert.Equal(1.5f, gizmo.Drag(_view2D, Centre + new Vector2(62f, 0f), snap: 0.5f).Scale.X, 4); // 1.55 -> 1.5
    }

    [Fact]
    public void Grabbing_a_scale_stalk_exactly_on_the_origin_gives_no_change_instead_of_dividing_by_zero()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre, _origin);

        Assert.Equal(GizmoDelta.None, gizmo.Drag(_view2D, Centre + new Vector2(50f, 0f)));
    }

    // ---- lifecycle ----

    [Fact]
    public void Dragging_before_begin_is_refused()
    {
        Assert.Throws<InvalidOperationException>(() => new TransformGizmo().Drag(_view2D, Centre));
    }

    [Fact]
    public void Beginning_without_a_handle_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new TransformGizmo().Begin(_view2D, GizmoHandle.None, Centre, _origin));
    }

    [Fact]
    public void End_stops_the_drag()
    {
        var gizmo = new TransformGizmo();
        gizmo.Begin(_view2D, GizmoHandle.AxisX, Centre + new Vector2(40f, 0f), _origin);
        Assert.True(gizmo.IsDragging);
        Assert.Equal(GizmoHandle.AxisX, gizmo.Active);

        gizmo.End();

        Assert.False(gizmo.IsDragging);
        Assert.Equal(GizmoHandle.None, gizmo.Active);
    }

    [Fact]
    public void A_ring_needs_enough_points()
    {
        Assert.Throws<ArgumentException>(() => TransformGizmo.TryGetRing(_view2D, _origin, 2, new Vector2[3]));
    }

    private static float AngleAboutZ(Quaternion rotation)
    {
        // Rotation about Z only: the angle is twice the atan2 of the z and w parts.
        return 2f * MathF.Atan2(rotation.Z, rotation.W);
    }
}
