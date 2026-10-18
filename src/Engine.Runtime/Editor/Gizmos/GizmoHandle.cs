namespace Engine.Runtime.Editor.Gizmos;

/// <summary>Which part of the transform gizmo the mouse is over or dragging.</summary>
public enum GizmoHandle
{
    /// <summary>None: the mouse is not on the gizmo.</summary>
    None,

    /// <summary>The red handle: the X axis (an arrow to move along, a ring to turn about, a stalk to scale along).</summary>
    AxisX,

    /// <summary>The green handle: the Y axis.</summary>
    AxisY,

    /// <summary>The blue handle: the Z axis (the only rotation handle in 2D).</summary>
    AxisZ,

    /// <summary>The square at the origin: moves freely in the screen plane, or scales on all axes at once. There is none in rotate mode.</summary>
    Free,
}
