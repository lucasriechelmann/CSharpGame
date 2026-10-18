using System.Numerics;

namespace Engine.Runtime.Editor.Gizmos;

/// <summary>
/// The logic of the transform gizmo: which handle the mouse is over (measured in screen space, so handles stay the same size at any zoom or
/// distance) and how a drag turns into a world-space change (<see cref="GizmoDelta"/>). It draws nothing and touches no entity, so it is
/// unit-tested; <c>SceneEditor</c> draws it and applies the result. One class serves translate, rotate and scale, in the 2D view and the 3D one.
/// </summary>
/// <remarks>
/// <para>Translate: arrows along the axes (drag keeps the point on the axis closest to the mouse under it) and a square that moves in the plane
/// facing the camera. Rotate: one ring per axis (drag turns by the angle the mouse sweeps around the ring's centre). Scale: stalks with cubes
/// along the entity's own axes (drag scales by how far the mouse is from the centre compared with where it started) and a centre cube for a
/// uniform scale. In the 2D view only X, Y and the Z ring exist.</para>
/// <para>Split by topic: this file (state and hit testing) and <c>TransformGizmo.Drag.cs</c>.</para>
/// </remarks>
public sealed partial class TransformGizmo
{
    /// <summary>Length of each axis arrow or stalk, in screen pixels.</summary>
    public const float ArrowLength = 80f;

    /// <summary>Radius of each rotation ring, in screen pixels.</summary>
    public const float RingRadius = 64f;

    /// <summary>Half the side of the centre square, in screen pixels.</summary>
    public const float FreeHalfSize = 7f;

    /// <summary>How close, in screen pixels, the mouse must be to a handle's line to grab it.</summary>
    public const float GrabDistance = 6f;

    /// <summary>How many segments a rotation ring is drawn and tested with.</summary>
    public const int RingSegments = 48;

    /// <summary>What dragging does. Changing it while dragging is not supported (end the drag first).</summary>
    public GizmoMode Mode { get; set; } = GizmoMode.Translate;

    /// <summary>Which axes translate and rotate handles follow. Scale always uses the entity's own axes.</summary>
    public GizmoSpace Space { get; set; } = GizmoSpace.World;

    /// <summary>True between <see cref="Begin"/> and <see cref="End"/>.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>The handle being dragged; <see cref="GizmoHandle.None"/> when not dragging.</summary>
    public GizmoHandle Active { get; private set; }

    /// <summary>
    /// The gizmo's frame for an entity: world axes for translate and rotate in world space, the entity's own axes in local space and
    /// always for scale (scale lives on the entity's axes, so dragging a world axis would be meaningless for a turned entity).
    /// </summary>
    /// <param name="origin">The entity's world position (or the middle of the selection).</param>
    /// <param name="worldRotation">The entity's world rotation.</param>
    public GizmoFrame FrameFor(Vector3 origin, Quaternion worldRotation) =>
        new(origin, Mode == GizmoMode.Scale || Space == GizmoSpace.Local ? worldRotation : Quaternion.Identity);

    /// <summary>The axis indices that exist in <paramref name="view"/>: all three in 3D; in 2D X and Y to move or scale, only Z to rotate.</summary>
    /// <param name="view">The view the gizmo is seen in.</param>
    /// <param name="first">The first axis index.</param>
    /// <param name="last">One past the last axis index.</param>
    public void AxisRange(IGizmoView view, out int first, out int last)
    {
        if (!view.IsOrthographic2D) (first, last) = (0, 3);
        else if (Mode == GizmoMode.Rotate) (first, last) = (2, 3);
        else (first, last) = (0, 2);
    }

    /// <summary>
    /// The handle under the mouse. The centre square wins over the axes where they overlap at the origin; among axes the nearest to the
    /// mouse wins, so overlapping rings or foreshortened arrows still resolve to the one the mouse is closest to.
    /// </summary>
    /// <param name="view">The view the gizmo is seen in.</param>
    /// <param name="mouse">Mouse position in screen pixels.</param>
    /// <param name="frame">Where the gizmo stands and which way its axes point.</param>
    public GizmoHandle HitTest(IGizmoView view, Vector2 mouse, in GizmoFrame frame)
    {
        if (!view.TryProject(frame.Origin, out var origin)) return GizmoHandle.None;

        if (Mode != GizmoMode.Rotate && MathF.Abs(mouse.X - origin.X) <= FreeHalfSize && MathF.Abs(mouse.Y - origin.Y) <= FreeHalfSize)
            return GizmoHandle.Free;

        var best = GizmoHandle.None;
        float bestDistance = GrabDistance;
        AxisRange(view, out int first, out int last);
        Span<Vector2> ring = stackalloc Vector2[RingSegments + 1];
        for (int axis = first; axis < last; axis++)
        {
            float distance;
            if (Mode == GizmoMode.Rotate)
            {
                distance = TryGetRing(view, frame, axis, ring) ? DistanceToPolyline(mouse, ring) : float.MaxValue;
            }
            else
            {
                distance = TryGetStalk(view, frame, axis, out var from, out var to)
                    ? GizmoMath.DistanceToSegment(mouse, from, to)
                    : float.MaxValue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = HandleOf(axis);
            }
        }

        return best;
    }

    /// <summary>The screen-space end points of an axis arrow or stalk; false when the origin is behind the camera.</summary>
    /// <param name="view">The view the gizmo is seen in.</param>
    /// <param name="frame">Where the gizmo stands and which way its axes point.</param>
    /// <param name="axis">0, 1 or 2.</param>
    /// <param name="from">The origin on screen.</param>
    /// <param name="to">The tip on screen.</param>
    public static bool TryGetStalk(IGizmoView view, in GizmoFrame frame, int axis, out Vector2 from, out Vector2 to)
    {
        to = default;
        if (!view.TryProject(frame.Origin, out from)) return false;

        float length = ArrowLength * view.WorldUnitsPerPixel(frame.Origin);
        return view.TryProject(frame.Origin + frame.Axis(axis) * length, out to);
    }

    /// <summary>
    /// Fills <paramref name="points"/> with the screen positions of a rotation ring around <paramref name="axis"/> (closed: the last point
    /// repeats the first). False when any point is behind the camera, where a ring would be drawn wrongly.
    /// </summary>
    /// <param name="view">The view the gizmo is seen in.</param>
    /// <param name="frame">Where the gizmo stands and which way its axes point.</param>
    /// <param name="axis">0, 1 or 2.</param>
    /// <param name="points">Receives <see cref="RingSegments"/> + 1 points.</param>
    public static bool TryGetRing(IGizmoView view, in GizmoFrame frame, int axis, Span<Vector2> points)
    {
        if (points.Length < RingSegments + 1) throw new ArgumentException($"A ring needs {RingSegments + 1} points.", nameof(points));

        float radius = RingRadius * view.WorldUnitsPerPixel(frame.Origin);
        var u = frame.Axis((axis + 1) % 3);
        var v = frame.Axis((axis + 2) % 3);
        for (int i = 0; i <= RingSegments; i++)
        {
            float angle = MathF.Tau * i / RingSegments;
            var world = frame.Origin + (u * MathF.Cos(angle) + v * MathF.Sin(angle)) * radius;
            if (!view.TryProject(world, out points[i])) return false;
        }

        return true;
    }

    /// <summary>The handle for axis 0 (X), 1 (Y) or 2 (Z).</summary>
    /// <param name="axis">The axis index.</param>
    public static GizmoHandle HandleOf(int axis) => axis switch
    {
        0 => GizmoHandle.AxisX,
        1 => GizmoHandle.AxisY,
        2 => GizmoHandle.AxisZ,
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "An axis index is 0, 1 or 2."),
    };

    /// <summary>The axis index of an axis handle.</summary>
    /// <param name="handle">An axis handle (not <see cref="GizmoHandle.None"/> or <see cref="GizmoHandle.Free"/>).</param>
    public static int AxisOf(GizmoHandle handle) => handle switch
    {
        GizmoHandle.AxisX => 0,
        GizmoHandle.AxisY => 1,
        GizmoHandle.AxisZ => 2,
        _ => throw new ArgumentException($"{handle} is not an axis handle.", nameof(handle)),
    };

    private static float DistanceToPolyline(Vector2 point, ReadOnlySpan<Vector2> points)
    {
        float best = float.MaxValue;
        for (int i = 0; i + 1 < points.Length; i++)
            best = MathF.Min(best, GizmoMath.DistanceToSegment(point, points[i], points[i + 1]));
        return best;
    }
}
