using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Rendering;
using Engine.Runtime.Editor.Undo;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Runtime.Editor;

// The scene view: mouse picking, the move gizmo, panning and zooming, and the overlay drawn over the scene.
internal sealed partial class SceneEditor
{
    /// <summary>Grid, in world units, that the gizmo snaps to while Ctrl is held.</summary>
    private const float SnapSize = 8f;

    private readonly MoveGizmo _gizmo = new();
    private object? _dragStartTransform;
    private GizmoHandle _hover;

    /// <summary>Handles the mouse over the scene (not over an editor window).</summary>
    private void HandleViewport()
    {
        var camera = _hooks.Camera();
        if (camera == null) return;

        var io = ImGui.GetIO();
        var mouse = ImGui.GetMousePos();
        var selected = _inspector.Selected;

        if (_gizmo.IsDragging)
        {
            if (!selected.IsNull && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                MoveSelected(selected, _gizmo.Drag(camera.ScreenToWorld(mouse), CtrlHeld() ? SnapSize : 0f));
            else
                EndGizmoDrag(selected);
            return;
        }

        _hover = GizmoHandle.None;

        // Paint mode: the mouse paints tiles instead of selecting. A stroke keeps going when the mouse strays over a window.
        if (_tiles.IsPainting || _tiles.IsStroking)
        {
            _tiles.HandleMouse(camera, mouse, io.WantCaptureMouse);
            if (io.WantCaptureMouse) return;

            ZoomAndPan(camera, io, mouse);
            _tiles.DrawCursor(camera, mouse);
            return;
        }

        if (io.WantCaptureMouse) return; // over a window or menu

        var selectedPosition = Vector2.Zero;
        if (!selected.IsNull && _world.TryGet<Transform>(selected, out var transform))
        {
            var worldPosition = TransformHierarchy.GetWorldPosition(_world, selected, transform); // a child sits where its parent puts it
            selectedPosition = new Vector2(worldPosition.X, worldPosition.Y);
            _hover = MoveGizmo.HitTest(mouse, camera.WorldToScreen(selectedPosition));
        }

        ZoomAndPan(camera, io, mouse);

        if (!ImGui.IsMouseClicked(ImGuiMouseButton.Left)) return;

        if (_hover != GizmoHandle.None)
        {
            _gizmo.Begin(_hover, camera.ScreenToWorld(mouse), selectedPosition);
            _dragStartTransform = _world.GetBoxed(selected, typeof(Transform));
            return;
        }

        float radius = EntityPicking.FallbackRadiusPixels / camera.Zoom;
        _inspector.Selected = EntityPicking.Pick(_world, camera.ScreenToWorld(mouse), radius);
    }

    /// <summary>Mouse wheel zooms about the cursor; the middle button drags the view.</summary>
    private static void ZoomAndPan(Camera2D camera, ImGuiIOPtr io, Vector2 mouse)
    {
        EditorCameraControls.ZoomAt(camera, mouse, io.MouseWheel);
        if (ImGui.IsMouseDown(ImGuiMouseButton.Middle)) EditorCameraControls.Pan(camera, io.MouseDelta);
    }

    /// <summary>Ends a gizmo drag and records it as one undo step (the world was already updated every frame of the drag).</summary>
    private void EndGizmoDrag(Entity entity)
    {
        _gizmo.End();
        var before = _dragStartTransform;
        _dragStartTransform = null;
        if (before == null || entity.IsNull) return;

        object after = _world.GetBoxed(entity, typeof(Transform));
        if (!Equals(before, after))
            _history.Push(new SetComponentCommand(_history.Context, entity, typeof(Transform), before, after, EntityLabel.Of(_world, entity)));
    }

    /// <summary>
    /// Moves the entity in X and Y (Z, the draw layer, is kept). Its render history is cleared so it is drawn at the new spot:
    /// the simulation is frozen, so an old history would leave it drawn part-way between where it was and where it is.
    /// </summary>
    private void MoveSelected(Entity entity, Vector2 position)
    {
        ref var transform = ref _world.Get<Transform>(entity);
        var parent = TransformHierarchy.GetParent(_world, entity);
        if (parent.IsNull)
        {
            transform.Position = new Vector3(position, transform.Position.Z);
        }
        else
        {
            // The gizmo works in world space; a child stores its position relative to the parent, so convert before writing.
            var placement = TransformHierarchy.GetWorldTransform(_world, entity);
            placement.Position = new Vector3(position, placement.Position.Z);
            transform.Position = TransformHierarchy.ToLocal(_world, parent, placement).Position;
        }

        if (_world.Has<PreviousTransform>(entity)) _world.Get<PreviousTransform>(entity) = default;
    }

    /// <summary>Draws the selection outline and the gizmo behind the editor windows, over the scene.</summary>
    private void DrawSceneOverlay()
    {
        var camera = _hooks.Camera();
        var selected = _inspector.Selected;
        if (camera == null || selected.IsNull || !_world.TryGet<Transform>(selected, out var transform)) return;

        var draw = ImGui.GetBackgroundDrawList();

        Span<Vector2> corners = stackalloc Vector2[4];
        if (EntityPicking.TryGetOutline(_world, selected, corners))
        {
            draw.AddQuad(
                camera.WorldToScreen(corners[0]), camera.WorldToScreen(corners[1]),
                camera.WorldToScreen(corners[2]), camera.WorldToScreen(corners[3]),
                ImGui.GetColorU32(new Vector4(1f, 0.85f, 0.2f, 1f)), 2f);
        }

        var worldOrigin = TransformHierarchy.GetWorldPosition(_world, selected, transform);
        var origin = camera.WorldToScreen(new Vector2(worldOrigin.X, worldOrigin.Y));
        var active = _gizmo.IsDragging ? _gizmo.Active : _hover;
        DrawArrow(draw, origin, new Vector2(MoveGizmo.ArrowLength, 0f), HandleColor(active == GizmoHandle.AxisX, 0.95f, 0.3f, 0.3f));
        DrawArrow(draw, origin, new Vector2(0f, -MoveGizmo.ArrowLength), HandleColor(active == GizmoHandle.AxisY, 0.35f, 0.9f, 0.35f));

        var half = new Vector2(MoveGizmo.FreeHalfSize);
        draw.AddRectFilled(origin - half, origin + half, HandleColor(active == GizmoHandle.Free, 1f, 0.85f, 0.2f));
    }

    /// <summary>A line with a small arrowhead from <paramref name="origin"/> along <paramref name="offset"/> (screen pixels).</summary>
    private static void DrawArrow(ImDrawListPtr draw, Vector2 origin, Vector2 offset, uint color)
    {
        var tip = origin + offset;
        var direction = Vector2.Normalize(offset);
        var side = new Vector2(-direction.Y, direction.X) * 5f;
        draw.AddLine(origin, tip, color, 2.5f);
        draw.AddTriangleFilled(tip + direction * 8f, tip - direction * 2f + side, tip - direction * 2f - side, color);
    }

    /// <summary>The handle colour, brightened while the handle is hovered or dragged.</summary>
    private static uint HandleColor(bool highlighted, float r, float g, float b) =>
        ImGui.GetColorU32(highlighted ? new Vector4(MathF.Min(r + 0.35f, 1f), MathF.Min(g + 0.35f, 1f), MathF.Min(b + 0.35f, 1f), 1f) : new Vector4(r, g, b, 0.9f));
}
