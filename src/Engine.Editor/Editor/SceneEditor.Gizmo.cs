using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Editor.Gizmos;
using Engine.Runtime.Editing;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Editor;

// The transform gizmo, shared by the 2D and 3D views: starting a drag, applying it to every selected entity, recording it as one undo step
// and drawing the handles.
internal sealed partial class SceneEditor
{
    /// <summary>Move grid in the 2D view while Ctrl is held, in world units (pixels at zoom 1).</summary>
    private const float SnapMove2D = 8f;

    /// <summary>Move grid in the 3D view while Ctrl is held, in world units (metres).</summary>
    private const float SnapMove3D = 0.5f;

    /// <summary>Rotation step while Ctrl is held: 15 degrees.</summary>
    private const float SnapRotate = MathF.PI / 12f;

    /// <summary>Scale step while Ctrl is held.</summary>
    private const float SnapScale = 0.1f;

    private readonly TransformGizmo _gizmo = new();
    private readonly List<GizmoTarget> _targets = new();
    private readonly List<Entity> _topLevel = new();
    private GizmoHandle _hover;
    private Vector3 _dragPivot;

    /// <summary>An entity being dragged: where it started and the value it had, kept for the undo step.</summary>
    private readonly record struct GizmoTarget(Entity Entity, GizmoStart Start, object Before);

    /// <summary>
    /// Where the gizmo stands for the current selection: at the entity, or at the average position of the selected entities that have no
    /// selected ancestor, with the primary entity's orientation (for local space and scale). False when nothing movable is selected.
    /// </summary>
    private bool TryGetGizmoFrame(out GizmoFrame frame)
    {
        _inspector.Selection.GetTopLevel(_world, _topLevel);
        var sum = Vector3.Zero;
        int count = 0;
        foreach (var entity in _topLevel)
        {
            if (!_world.TryGet<Transform>(entity, out var transform)) continue;

            sum += TransformHierarchy.GetWorldPosition(_world, entity, transform);
            count++;
        }

        if (count == 0)
        {
            frame = default;
            return false;
        }

        frame = _gizmo.FrameFor(sum / count, PrimaryWorldRotation());
        return true;
    }

    /// <summary>The primary entity's world rotation; identity when it has none or its matrix has no rotation (a zero scale).</summary>
    private Quaternion PrimaryWorldRotation()
    {
        var primary = _inspector.Selected;
        if (primary.IsNull || !_world.Has<Transform>(primary)) return Quaternion.Identity;

        return Matrix4x4.Decompose(TransformHierarchy.GetWorldMatrix(_world, primary), out _, out var rotation, out _)
            ? rotation
            : Quaternion.Identity;
    }

    /// <summary>
    /// Tests the mouse against the gizmo and starts a drag when the left button was just pressed on a handle.
    /// </summary>
    /// <returns>True when a drag started, so the caller must not treat the click as a selection click.</returns>
    private bool TryStartGizmoDrag(IGizmoView view, Vector2 mouse)
    {
        _hover = GizmoHandle.None;
        if (!TryGetGizmoFrame(out var frame)) return false;

        _hover = _gizmo.HitTest(view, mouse, frame);
        if (_hover == GizmoHandle.None || !ImGui.IsMouseClicked(ImGuiMouseButton.Left)) return false;

        _inspector.CommitPendingEdit();
        _targets.Clear();
        foreach (var entity in _topLevel)
        {
            if (_world.Has<Transform>(entity))
                _targets.Add(new GizmoTarget(entity, GizmoStart.Capture(_world, entity), _world.GetBoxed(entity, typeof(Transform))));
        }

        _dragPivot = frame.Origin;
        _gizmo.Begin(view, _hover, mouse, frame);
        return true;
    }

    /// <summary>Applies the drag to every dragged entity while the button is down, and ends it (as one undo step) when it is released.</summary>
    private void UpdateGizmoDrag(IGizmoView view, Vector2 mouse)
    {
        bool alive = true;
        foreach (var target in _targets) alive &= _world.IsAlive(target.Entity);

        if (!alive || !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            EndGizmoDrag();
            return;
        }

        var delta = _gizmo.Drag(view, mouse, CtrlHeld() ? SnapStep(view) : 0f);
        foreach (var target in _targets)
        {
            _world.Get<Transform>(target.Entity) = GizmoApply.Apply(_gizmo.Mode, target.Start, delta, _dragPivot);

            // The simulation is frozen, so an old render history would leave the entity drawn part-way back to where it was.
            if (_world.Has<PreviousTransform>(target.Entity)) _world.Get<PreviousTransform>(target.Entity) = default;
        }
    }

    private float SnapStep(IGizmoView view) => _gizmo.Mode switch
    {
        GizmoMode.Translate => view.IsOrthographic2D ? SnapMove2D : SnapMove3D,
        GizmoMode.Rotate => SnapRotate,
        _ => SnapScale,
    };

    /// <summary>Ends a gizmo drag and records it as one undo step (the world was already updated every frame of the drag).</summary>
    private void EndGizmoDrag()
    {
        _gizmo.End();
        var commands = new List<IEditCommand>(_targets.Count);
        foreach (var target in _targets)
        {
            if (!_world.IsAlive(target.Entity)) continue;

            object after = _world.GetBoxed(target.Entity, typeof(Transform));
            if (!Equals(target.Before, after))
                commands.Add(new SetComponentCommand(_history.Context, target.Entity, typeof(Transform), target.Before, after, EntityLabel.Of(_world, target.Entity)));
        }

        _targets.Clear();
        if (commands.Count == 1) _history.Push(commands[0]);
        else if (commands.Count > 1) _history.Push(new CompositeCommand($"Move {commands.Count} entities", commands, 0));
    }

    private void SetGizmoMode(GizmoMode mode)
    {
        if (_gizmo.IsDragging) return; // the drag's maths belong to the mode it started in
        _gizmo.Mode = mode;
    }

    private void ToggleGizmoSpace()
    {
        if (_gizmo.IsDragging) return;
        _gizmo.Space = _gizmo.Space == GizmoSpace.World ? GizmoSpace.Local : GizmoSpace.World;
    }

    /// <summary>Drops a drag in progress without recording it (the editor is closing or Play started).</summary>
    private void AbortGizmoDrag()
    {
        _gizmo.End();
        _targets.Clear();
    }

    /// <summary>Draws the handles for the current mode behind the editor windows.</summary>
    private void DrawGizmo(ImDrawListPtr draw, IGizmoView view)
    {
        if (!TryGetGizmoFrame(out var frame) || !view.TryProject(frame.Origin, out var origin)) return;

        var active = _gizmo.IsDragging ? _gizmo.Active : _hover;
        _gizmo.AxisRange(view, out int first, out int last);
        Span<Vector2> ring = stackalloc Vector2[TransformGizmo.RingSegments + 1];
        for (int axis = first; axis < last; axis++)
        {
            uint colour = AxisColour(axis, active == TransformGizmo.HandleOf(axis));
            if (_gizmo.Mode == GizmoMode.Rotate)
            {
                if (!TransformGizmo.TryGetRing(view, frame, axis, ring)) continue;
                for (int i = 0; i < TransformGizmo.RingSegments; i++) draw.AddLine(ring[i], ring[i + 1], colour, 2.5f);
                continue;
            }

            if (!TransformGizmo.TryGetStalk(view, frame, axis, out var from, out var to)) continue;
            draw.AddLine(from, to, colour, 2.5f);
            if (_gizmo.Mode == GizmoMode.Translate) DrawArrowHead(draw, from, to, colour);
            else draw.AddRectFilled(to - new Vector2(5f), to + new Vector2(5f), colour);
        }

        if (_gizmo.Mode != GizmoMode.Rotate)
        {
            var half = new Vector2(TransformGizmo.FreeHalfSize);
            uint centre = ImGui.GetColorU32(active == GizmoHandle.Free ? new Vector4(1f, 1f, 0.6f, 1f) : new Vector4(1f, 0.85f, 0.2f, 0.9f));
            draw.AddRectFilled(origin - half, origin + half, centre);
        }
    }

    private static void DrawArrowHead(ImDrawListPtr draw, Vector2 from, Vector2 tip, uint colour)
    {
        var offset = tip - from;
        if (offset.LengthSquared() < 16f) return; // foreshortened to a dot: nothing to point with

        var direction = Vector2.Normalize(offset);
        var side = new Vector2(-direction.Y, direction.X) * 5f;
        draw.AddTriangleFilled(tip + direction * 8f, tip - direction * 2f + side, tip - direction * 2f - side, colour);
    }

    /// <summary>Red for X, green for Y, blue for Z; brightened while the handle is hovered or dragged.</summary>
    private static uint AxisColour(int axis, bool highlighted)
    {
        var (r, g, b) = axis switch
        {
            0 => (0.95f, 0.3f, 0.3f),
            1 => (0.35f, 0.9f, 0.35f),
            _ => (0.35f, 0.55f, 1f),
        };
        return ImGui.GetColorU32(highlighted
            ? new Vector4(MathF.Min(r + 0.35f, 1f), MathF.Min(g + 0.35f, 1f), MathF.Min(b + 0.35f, 1f), 1f)
            : new Vector4(r, g, b, 0.9f));
    }
}
