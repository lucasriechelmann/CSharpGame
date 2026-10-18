using System.Numerics;
using Engine.Core.Ecs;
using Engine.Rendering;
using Engine.Runtime.Editor.Gizmos;
using ImGuiNET;

namespace Engine.Runtime.Editor;

// The 2D scene view: picking, panning and zooming, tile painting, and the selection overlay. The gizmo is shared with the 3D view
// (SceneEditor.Gizmo.cs).
internal sealed partial class SceneEditor
{
    /// <summary>Handles the mouse over the scene (not over an editor window).</summary>
    private void HandleViewport()
    {
        var camera3D = _hooks.Camera3D();
        if (camera3D != null)
        {
            HandleViewport3D(camera3D);
            return;
        }

        var camera = _hooks.Camera();
        if (camera == null) return;

        var io = ImGui.GetIO();
        var mouse = ImGui.GetMousePos();
        var view = new OrthographicGizmoView(camera);

        if (_gizmo.IsDragging)
        {
            UpdateGizmoDrag(view, mouse);
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

        ZoomAndPan(camera, io, mouse);

        if (TryStartGizmoDrag(view, mouse) || !ImGui.IsMouseClicked(ImGuiMouseButton.Left)) return;

        float radius = EntityPicking.FallbackRadiusPixels / camera.Zoom;
        ApplyPick(EntityPicking.Pick(_world, camera.ScreenToWorld(mouse), radius));
    }

    /// <summary>A click in the scene: selects what was hit (Ctrl adds it to or takes it out of the selection); an empty click clears, unless Ctrl is held.</summary>
    private void ApplyPick(Entity hit)
    {
        if (CtrlHeld()) _inspector.Selection.Toggle(hit);
        else _inspector.Selected = hit;
    }

    /// <summary>Mouse wheel zooms about the cursor; the middle button drags the view.</summary>
    private static void ZoomAndPan(Camera2D camera, ImGuiIOPtr io, Vector2 mouse)
    {
        EditorCameraControls.ZoomAt(camera, mouse, io.MouseWheel);
        if (ImGui.IsMouseDown(ImGuiMouseButton.Middle)) EditorCameraControls.Pan(camera, io.MouseDelta);
    }

    /// <summary>Draws the selection outlines and the gizmo behind the editor windows, over the scene.</summary>
    private void DrawSceneOverlay()
    {
        var camera3D = _hooks.Camera3D();
        if (camera3D != null)
        {
            DrawSceneOverlay3D(camera3D);
            return;
        }

        var camera = _hooks.Camera();
        if (camera == null || _inspector.Selection.Count == 0) return;

        var draw = ImGui.GetBackgroundDrawList();
        Span<Vector2> corners = stackalloc Vector2[4];
        foreach (var entity in _inspector.Selection.Items)
        {
            if (!EntityPicking.TryGetOutline(_world, entity, corners)) continue;

            draw.AddQuad(
                camera.WorldToScreen(corners[0]), camera.WorldToScreen(corners[1]),
                camera.WorldToScreen(corners[2]), camera.WorldToScreen(corners[3]),
                OutlineColour(entity), 2f);
        }

        DrawGizmo(draw, new OrthographicGizmoView(camera));
    }

    /// <summary>The selection outline colour: bright for the primary, dimmer for the rest.</summary>
    private uint OutlineColour(Entity entity) =>
        ImGui.GetColorU32(entity == _inspector.Selected ? new Vector4(1f, 0.85f, 0.2f, 1f) : new Vector4(1f, 0.6f, 0.15f, 0.85f));
}
