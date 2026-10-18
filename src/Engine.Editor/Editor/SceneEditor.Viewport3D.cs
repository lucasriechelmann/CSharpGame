using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Rendering3D;
using Engine.Editor.Gizmos;
using ImGuiNET;

namespace Engine.Editor;

// The 3D scene view: the orbit camera, click-to-select by mesh bounds and the selection outlines.
internal sealed partial class SceneEditor
{
    /// <summary>Radians of orbit per pixel of mouse movement.</summary>
    private const float OrbitSensitivity = 0.005f;

    /// <summary>How far in front of the game's camera the orbit centre starts, so the first editor frame looks like the game's last.</summary>
    private const float InitialOrbitDistance = 8f;

    private static readonly (int A, int B)[] BoxEdges =
    [
        (0, 1), (1, 2), (2, 3), (3, 0), // the min-Z face
        (4, 5), (5, 6), (6, 7), (7, 4), // the max-Z face
        (0, 4), (1, 5), (2, 6), (3, 7), // the edges joining them
    ];

    private readonly List<PickBox> _boxScratch = new();
    private OrbitCamera? _orbit;

    /// <summary>
    /// Mouse and keyboard over the 3D scene: middle-drag (or Alt + left-drag) orbits, with Shift it pans, the wheel zooms, a left click on
    /// the gizmo drags it and a left click elsewhere selects what the mouse ray hits first (Ctrl adds to the selection). The orbit camera
    /// is written to the game's camera every frame, so the game's own fly camera must not be applying itself while editing (see
    /// <c>Game.SceneCamera3D</c>).
    /// </summary>
    private void HandleViewport3D(Camera3D camera)
    {
        _orbit ??= OrbitCamera.From(camera, InitialOrbitDistance);
        var io = ImGui.GetIO();
        var mouse = ImGui.GetMousePos();
        var view = new PerspectiveGizmoView(camera);

        if (_gizmo.IsDragging)
        {
            UpdateGizmoDrag(view, mouse);
            _orbit.ApplyTo(camera);
            return;
        }

        _hover = GizmoHandle.None;
        if (!io.WantCaptureMouse)
        {
            bool alt = ImGui.IsKeyDown(ImGuiKey.LeftAlt) || ImGui.IsKeyDown(ImGuiKey.RightAlt);
            bool shift = ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);
            bool dragging = ImGui.IsMouseDown(ImGuiMouseButton.Middle) || (alt && ImGui.IsMouseDown(ImGuiMouseButton.Left));
            if (dragging)
            {
                if (shift) _orbit.Pan(io.MouseDelta, camera.FovY, camera.ViewportSize.Y);
                else _orbit.Orbit(-io.MouseDelta.X * OrbitSensitivity, -io.MouseDelta.Y * OrbitSensitivity); // drag right = turn right = negative yaw, like the fly camera
            }

            _orbit.Zoom(io.MouseWheel);

            if (!alt && !TryStartGizmoDrag(view, mouse) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                PickAt(camera, mouse);
        }

        _orbit.ApplyTo(camera);
    }

    /// <summary>Selects the entity under a screen point (Ctrl adds it to the selection or takes it out), or nothing when the ray hits no entity.</summary>
    private void PickAt(Camera3D camera, Vector2 screenPoint)
    {
        var ray = camera.ScreenPointToRay(screenPoint);
        ApplyPick(EntityPicking3D.Pick(_world, ray, camera, _boxes.Collect, out _));
    }

    /// <summary>Frames the selection: 3D re-centres the orbit camera on its bounds, 2D moves the camera to its middle.</summary>
    private void FocusSelected()
    {
        if (_inspector.Selection.Count == 0) return;

        var camera3D = _hooks.Camera3D();
        if (camera3D != null)
        {
            _orbit ??= OrbitCamera.From(camera3D, InitialOrbitDistance);
            var (centre, radius) = SelectionSphere();
            _orbit.FocusOn(centre, radius, camera3D.FovY);
            return;
        }

        var camera2D = _hooks.Camera();
        if (camera2D != null && TryGetGizmoFrame(out var frame))
            camera2D.Position = new Vector2(frame.Origin.X, frame.Origin.Y);
    }

    /// <summary>The centre and radius of a sphere around everything selected (the boxes of what they draw, or their positions when they draw nothing).</summary>
    private (Vector3 Centre, float Radius) SelectionSphere()
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var entity in _inspector.Selection.Items)
        {
            _boxScratch.Clear();
            _boxes.Collect(entity, _boxScratch);
            if (_boxScratch.Count == 0 && _world.TryGet<Transform>(entity, out var transform))
            {
                var position = TransformHierarchy.GetWorldPosition(_world, entity, transform);
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);
                continue;
            }

            foreach (var box in _boxScratch)
            {
                EntityPicking3D.GetCorners(box, corners);
                foreach (var corner in corners)
                {
                    min = Vector3.Min(min, corner);
                    max = Vector3.Max(max, corner);
                }
            }
        }

        return min.X > max.X ? (Vector3.Zero, 0f) : ((min + max) * 0.5f, (max - min).Length() * 0.5f);
    }

    /// <summary>Draws the selection outlines (each entity's oriented boxes, or a cross for one with no mesh) and the gizmo behind the editor windows.</summary>
    private void DrawSceneOverlay3D(Camera3D camera)
    {
        if (_inspector.Selection.Count == 0) return;

        var draw = ImGui.GetBackgroundDrawList();
        Span<Vector3> corners = stackalloc Vector3[8];
        Span<Vector2> screen = stackalloc Vector2[8];
        Span<bool> visible = stackalloc bool[8];
        foreach (var entity in _inspector.Selection.Items)
        {
            uint colour = OutlineColour(entity);
            _boxScratch.Clear();
            _boxes.Collect(entity, _boxScratch);
            foreach (var box in _boxScratch)
            {
                EntityPicking3D.GetCorners(box, corners);
                for (int i = 0; i < 8; i++) visible[i] = camera.TryWorldToScreen(corners[i], out screen[i]);
                foreach (var (a, b) in BoxEdges)
                {
                    if (visible[a] && visible[b]) draw.AddLine(screen[a], screen[b], colour, 2f);
                }
            }

            if (_boxScratch.Count == 0 && _world.TryGet<Transform>(entity, out var transform)
                && camera.TryWorldToScreen(TransformHierarchy.GetWorldPosition(_world, entity, transform), out var point))
            {
                draw.AddCircle(point, 9f, colour, 16, 2f);
                draw.AddLine(point - new Vector2(13f, 0f), point + new Vector2(13f, 0f), colour, 1.5f);
                draw.AddLine(point - new Vector2(0f, 13f), point + new Vector2(0f, 13f), colour, 1.5f);
            }
        }

        DrawGizmo(draw, new PerspectiveGizmoView(camera));
    }
}
