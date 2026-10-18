using System.Numerics;

namespace Engine.Runtime.Editor;

/// <summary>
/// The logic of the move gizmo: which handle the mouse is over (in screen space, so the handles stay the same size at any zoom)
/// and how a drag turns into a new entity position (in world space). It draws nothing and touches no entity, so it is unit-tested;
/// <c>SceneEditor</c> draws it and applies the result.
/// </summary>
/// <remarks>
/// The gizmo assumes an unrotated editor camera: world +X is screen right and world +Y is screen up.
/// </remarks>
public sealed class MoveGizmo
{
    /// <summary>Length of each axis arrow, in screen pixels.</summary>
    public const float ArrowLength = 64f;

    /// <summary>Half the side of the free-move square at the origin, in screen pixels.</summary>
    public const float FreeHalfSize = 7f;

    /// <summary>How close, in screen pixels, the mouse must be to an arrow's line to grab it.</summary>
    public const float GrabDistance = 6f;

    private Vector2 _grabOffset;
    private Vector2 _startPosition;

    /// <summary>True between <see cref="Begin"/> and <see cref="End"/>.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>The handle being dragged; <see cref="GizmoHandle.None"/> when not dragging.</summary>
    public GizmoHandle Active { get; private set; }

    /// <summary>
    /// The handle under the mouse. The free square wins over the arrows where they overlap at the origin.
    /// </summary>
    /// <param name="mouseScreen">Mouse position in screen pixels.</param>
    /// <param name="originScreen">The entity's position in screen pixels.</param>
    public static GizmoHandle HitTest(Vector2 mouseScreen, Vector2 originScreen)
    {
        var delta = mouseScreen - originScreen;
        if (MathF.Abs(delta.X) <= FreeHalfSize && MathF.Abs(delta.Y) <= FreeHalfSize) return GizmoHandle.Free;

        // X arrow: a horizontal segment to the right; Y arrow: a vertical segment going up the screen (negative screen Y).
        if (delta.X > 0f && delta.X <= ArrowLength && MathF.Abs(delta.Y) <= GrabDistance) return GizmoHandle.AxisX;
        if (delta.Y < 0f && -delta.Y <= ArrowLength && MathF.Abs(delta.X) <= GrabDistance) return GizmoHandle.AxisY;
        return GizmoHandle.None;
    }

    /// <summary>Starts dragging <paramref name="handle"/>.</summary>
    /// <param name="handle">The handle that was grabbed; must not be <see cref="GizmoHandle.None"/>.</param>
    /// <param name="mouseWorld">Where the mouse is in world space; the entity keeps its offset from this point while dragged.</param>
    /// <param name="entityPosition">The entity's position (X, Y) at the start of the drag.</param>
    public void Begin(GizmoHandle handle, Vector2 mouseWorld, Vector2 entityPosition)
    {
        if (handle == GizmoHandle.None) throw new ArgumentException("There is no handle to drag.", nameof(handle));

        IsDragging = true;
        Active = handle;
        _startPosition = entityPosition;
        _grabOffset = mouseWorld;
    }

    /// <summary>The entity position for the current mouse position.</summary>
    /// <param name="mouseWorld">Mouse position in world space.</param>
    /// <param name="snap">Grid size to round the moved coordinates to; zero or less leaves them free.</param>
    /// <exception cref="InvalidOperationException">No drag is in progress.</exception>
    public Vector2 Drag(Vector2 mouseWorld, float snap = 0f)
    {
        if (!IsDragging) throw new InvalidOperationException("Call Begin before Drag.");

        var moved = mouseWorld - _grabOffset;
        if (Active == GizmoHandle.AxisX) moved.Y = 0f;
        else if (Active == GizmoHandle.AxisY) moved.X = 0f;

        var position = _startPosition + moved;
        if (snap > 0f)
        {
            // Snap only the axes that move, so dragging along X never nudges Y onto the grid.
            if (Active != GizmoHandle.AxisY) position.X = MathF.Round(position.X / snap) * snap;
            if (Active != GizmoHandle.AxisX) position.Y = MathF.Round(position.Y / snap) * snap;
        }

        return position;
    }

    /// <summary>Ends the drag.</summary>
    public void End()
    {
        IsDragging = false;
        Active = GizmoHandle.None;
    }
}
