namespace Engine.Runtime.Editor;

/// <summary>Which part of the move gizmo the mouse is over or dragging.</summary>
public enum GizmoHandle
{
    /// <summary>None: the mouse is not on the gizmo.</summary>
    None,

    /// <summary>The red arrow: moves along world X only.</summary>
    AxisX,

    /// <summary>The green arrow: moves along world Y only.</summary>
    AxisY,

    /// <summary>The square at the origin: moves freely on both axes.</summary>
    Free,
}
