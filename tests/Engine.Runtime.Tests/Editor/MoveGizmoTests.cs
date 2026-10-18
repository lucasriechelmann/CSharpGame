using System.Numerics;
using Engine.Runtime.Editor;

namespace Engine.Runtime.Tests.Editor;

public class MoveGizmoTests
{
    private static readonly Vector2 Origin = new(400f, 300f);

    [Fact]
    public void The_origin_square_is_the_free_handle()
    {
        Assert.Equal(GizmoHandle.Free, MoveGizmo.HitTest(Origin + new Vector2(3f, -4f), Origin));
    }

    [Fact]
    public void The_right_pointing_arrow_is_the_x_handle()
    {
        Assert.Equal(GizmoHandle.AxisX, MoveGizmo.HitTest(Origin + new Vector2(40f, 3f), Origin));
    }

    [Fact]
    public void The_upward_arrow_is_the_y_handle()
    {
        // Screen Y grows downward, so "up" is a negative offset.
        Assert.Equal(GizmoHandle.AxisY, MoveGizmo.HitTest(Origin + new Vector2(-2f, -40f), Origin));
    }

    [Theory]
    [InlineData(40f, 20f)]   // too far from the X arrow's line
    [InlineData(-40f, 0f)]   // left of the origin: no arrow there
    [InlineData(0f, 40f)]    // below the origin: the Y arrow points up
    [InlineData(MoveGizmo.ArrowLength + 5f, 0f)] // past the arrow's tip
    [InlineData(200f, 200f)]
    public void Elsewhere_is_no_handle(float dx, float dy)
    {
        Assert.Equal(GizmoHandle.None, MoveGizmo.HitTest(Origin + new Vector2(dx, dy), Origin));
    }

    [Fact]
    public void A_free_drag_moves_by_the_mouse_delta()
    {
        var gizmo = new MoveGizmo();
        gizmo.Begin(GizmoHandle.Free, new Vector2(100f, 100f), new Vector2(10f, 20f));

        var position = gizmo.Drag(new Vector2(130f, 90f));

        Assert.Equal(new Vector2(40f, 10f), position);
    }

    [Fact]
    public void The_entity_keeps_its_offset_from_where_it_was_grabbed()
    {
        var gizmo = new MoveGizmo();
        gizmo.Begin(GizmoHandle.Free, new Vector2(105f, 100f), new Vector2(100f, 100f)); // grabbed 5 right of the origin

        Assert.Equal(new Vector2(100f, 100f), gizmo.Drag(new Vector2(105f, 100f))); // no jump on the first frame
    }

    [Fact]
    public void An_x_drag_ignores_vertical_mouse_movement()
    {
        var gizmo = new MoveGizmo();
        gizmo.Begin(GizmoHandle.AxisX, Vector2.Zero, new Vector2(10f, 20f));

        Assert.Equal(new Vector2(40f, 20f), gizmo.Drag(new Vector2(30f, 99f)));
    }

    [Fact]
    public void A_y_drag_ignores_horizontal_mouse_movement()
    {
        var gizmo = new MoveGizmo();
        gizmo.Begin(GizmoHandle.AxisY, Vector2.Zero, new Vector2(10f, 20f));

        Assert.Equal(new Vector2(10f, 50f), gizmo.Drag(new Vector2(99f, 30f)));
    }

    [Fact]
    public void Snapping_rounds_the_moved_axes_to_the_grid()
    {
        var gizmo = new MoveGizmo();
        gizmo.Begin(GizmoHandle.Free, Vector2.Zero, Vector2.Zero);

        Assert.Equal(new Vector2(16f, -8f), gizmo.Drag(new Vector2(13f, -5f), snap: 8f));
    }

    [Fact]
    public void Snapping_an_axis_drag_leaves_the_other_coordinate_alone()
    {
        var gizmo = new MoveGizmo();
        gizmo.Begin(GizmoHandle.AxisX, Vector2.Zero, new Vector2(0f, 3f)); // y = 3 is off the 8-grid on purpose

        Assert.Equal(new Vector2(16f, 3f), gizmo.Drag(new Vector2(13f, 0f), snap: 8f));
    }

    [Fact]
    public void Dragging_state_follows_begin_and_end()
    {
        var gizmo = new MoveGizmo();
        Assert.False(gizmo.IsDragging);

        gizmo.Begin(GizmoHandle.AxisY, Vector2.Zero, Vector2.Zero);
        Assert.True(gizmo.IsDragging);
        Assert.Equal(GizmoHandle.AxisY, gizmo.Active);

        gizmo.End();
        Assert.False(gizmo.IsDragging);
        Assert.Equal(GizmoHandle.None, gizmo.Active);
    }

    [Fact]
    public void Dragging_without_beginning_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new MoveGizmo().Drag(Vector2.Zero));
    }

    [Fact]
    public void Beginning_on_no_handle_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new MoveGizmo().Begin(GizmoHandle.None, Vector2.Zero, Vector2.Zero));
    }
}
