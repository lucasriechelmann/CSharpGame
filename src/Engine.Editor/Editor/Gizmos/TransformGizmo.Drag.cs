using System.Numerics;

namespace Engine.Editor.Gizmos;

// Dragging: turning mouse movement into a world-space change.
public sealed partial class TransformGizmo
{
    /// <summary>Smallest scale factor a scale drag produces, so dragging through the centre cannot flip or collapse the entity.</summary>
    public const float MinScaleFactor = 0.01f;

    /// <summary>Screen distance below which a uniform scale drag started too close to the centre to give a stable ratio.</summary>
    private const float MinUniformReference = 8f;

    private GizmoFrame _frame;
    private float _startAxisParameter;
    private Vector3 _startPoint;
    private Vector3 _startVector;
    private float _startScreenDistance;
    private bool _startValid;

    /// <summary>Starts dragging <paramref name="handle"/>, remembering where the mouse grabbed it so there is no jump on the first frame.</summary>
    /// <param name="view">The view the gizmo is seen in.</param>
    /// <param name="handle">The handle that was grabbed; must not be <see cref="GizmoHandle.None"/>, nor <see cref="GizmoHandle.Free"/> in rotate mode.</param>
    /// <param name="mouse">Mouse position in screen pixels.</param>
    /// <param name="frame">Where the gizmo stands and which way its axes point; held until <see cref="End"/>.</param>
    public void Begin(IGizmoView view, GizmoHandle handle, Vector2 mouse, in GizmoFrame frame)
    {
        if (handle == GizmoHandle.None) throw new ArgumentException("There is no handle to drag.", nameof(handle));
        if (handle == GizmoHandle.Free && Mode == GizmoMode.Rotate) throw new ArgumentException("Rotation has no free handle.", nameof(handle));

        IsDragging = true;
        Active = handle;
        _frame = frame;
        _startValid = false;

        var ray = view.ScreenRay(mouse);
        if (handle == GizmoHandle.Free)
        {
            if (Mode == GizmoMode.Translate)
                _startValid = GizmoMath.TryIntersectPlane(ray, frame.Origin, -view.ViewForward, out _startPoint);
            else if (view.TryProject(frame.Origin, out var origin))
            {
                _startScreenDistance = MathF.Max(Vector2.Distance(mouse, origin), MinUniformReference);
                _startValid = true;
            }

            return;
        }

        var axis = frame.Axis(AxisOf(handle));
        switch (Mode)
        {
            case GizmoMode.Rotate:
                if (GizmoMath.TryIntersectPlane(ray, frame.Origin, axis, out var hit))
                {
                    _startVector = hit - frame.Origin;
                    _startValid = _startVector.LengthSquared() > 1e-10f;
                }

                break;
            default:
                _startValid = GizmoMath.TryClosestOnAxis(ray, frame.Origin, axis, out _startAxisParameter);
                break;
        }
    }

    /// <summary>The total change since <see cref="Begin"/> for the current mouse position.</summary>
    /// <param name="view">The view the gizmo is seen in.</param>
    /// <param name="mouse">Mouse position in screen pixels.</param>
    /// <param name="snap">
    /// Grid for the result: world units for translate, radians for rotate, a factor step for scale. Zero or less leaves it free.
    /// The change is snapped, not the final position, so an entity that is off the grid keeps its offset.
    /// </param>
    /// <exception cref="InvalidOperationException">No drag is in progress.</exception>
    public GizmoDelta Drag(IGizmoView view, Vector2 mouse, float snap = 0f)
    {
        if (!IsDragging) throw new InvalidOperationException("Call Begin before Drag.");
        if (!_startValid) return GizmoDelta.None; // grabbed where the geometry is degenerate (a ray parallel to the axis or plane)

        return Mode switch
        {
            GizmoMode.Translate => DragTranslate(view, mouse, snap),
            GizmoMode.Rotate => DragRotate(view, mouse, snap),
            _ => DragScale(view, mouse, snap),
        };
    }

    /// <summary>Ends the drag.</summary>
    public void End()
    {
        IsDragging = false;
        Active = GizmoHandle.None;
    }

    private GizmoDelta DragTranslate(IGizmoView view, Vector2 mouse, float snap)
    {
        var ray = view.ScreenRay(mouse);
        if (Active == GizmoHandle.Free)
        {
            if (!GizmoMath.TryIntersectPlane(ray, _frame.Origin, -view.ViewForward, out var point)) return GizmoDelta.None;

            var moved = point - _startPoint;
            if (snap > 0f) moved = SnapInPlane(moved, view.ViewForward, snap);
            return new GizmoDelta(moved, Quaternion.Identity, Vector3.One);
        }

        var axis = _frame.Axis(AxisOf(Active));
        if (!GizmoMath.TryClosestOnAxis(ray, _frame.Origin, axis, out float parameter)) return GizmoDelta.None;

        float distance = GizmoMath.Snap(parameter - _startAxisParameter, snap);
        return new GizmoDelta(axis * distance, Quaternion.Identity, Vector3.One);
    }

    private GizmoDelta DragRotate(IGizmoView view, Vector2 mouse, float snap)
    {
        var axis = _frame.Axis(AxisOf(Active));
        if (!GizmoMath.TryIntersectPlane(view.ScreenRay(mouse), _frame.Origin, axis, out var hit)) return GizmoDelta.None;

        var vector = hit - _frame.Origin;
        if (vector.LengthSquared() < 1e-10f) return GizmoDelta.None;

        float angle = GizmoMath.Snap(GizmoMath.SignedAngle(_startVector, vector, axis), snap);
        return new GizmoDelta(Vector3.Zero, Quaternion.CreateFromAxisAngle(axis, angle), Vector3.One);
    }

    private GizmoDelta DragScale(IGizmoView view, Vector2 mouse, float snap)
    {
        float factor;
        if (Active == GizmoHandle.Free)
        {
            if (!view.TryProject(_frame.Origin, out var origin)) return GizmoDelta.None;
            factor = Vector2.Distance(mouse, origin) / _startScreenDistance;
        }
        else
        {
            var axis = _frame.Axis(AxisOf(Active));
            if (!GizmoMath.TryClosestOnAxis(view.ScreenRay(mouse), _frame.Origin, axis, out float parameter)) return GizmoDelta.None;
            if (MathF.Abs(_startAxisParameter) < 1e-4f) return GizmoDelta.None; // grabbed on the origin: no ratio to speak of

            factor = parameter / _startAxisParameter;
        }

        factor = MathF.Max(GizmoMath.Snap(factor, snap), MinScaleFactor);
        if (Active == GizmoHandle.Free)
            return new GizmoDelta(Vector3.Zero, Quaternion.Identity, view.IsOrthographic2D ? new Vector3(factor, factor, 1f) : new Vector3(factor));

        var scale = Vector3.One;
        switch (AxisOf(Active))
        {
            case 0: scale.X = factor; break;
            case 1: scale.Y = factor; break;
            default: scale.Z = factor; break;
        }

        return new GizmoDelta(Vector3.Zero, Quaternion.Identity, scale);
    }

    /// <summary>Snaps a move in the plane facing the camera to the grid along the two world axes that plane is closest to (in 2D, X and Y).</summary>
    private static Vector3 SnapInPlane(Vector3 moved, Vector3 viewForward, float snap)
    {
        // Snap in the view plane's own two directions would depend on the camera angle; world axes are what a grid means.
        float ax = MathF.Abs(viewForward.X), ay = MathF.Abs(viewForward.Y), az = MathF.Abs(viewForward.Z);
        if (az >= ax && az >= ay) return new Vector3(GizmoMath.Snap(moved.X, snap), GizmoMath.Snap(moved.Y, snap), moved.Z);
        if (ay >= ax) return new Vector3(GizmoMath.Snap(moved.X, snap), moved.Y, GizmoMath.Snap(moved.Z, snap));
        return new Vector3(moved.X, GizmoMath.Snap(moved.Y, snap), GizmoMath.Snap(moved.Z, snap));
    }
}
