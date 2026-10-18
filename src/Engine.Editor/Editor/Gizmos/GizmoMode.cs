namespace Engine.Editor.Gizmos;

/// <summary>What dragging the gizmo does.</summary>
public enum GizmoMode
{
    /// <summary>Move (arrows).</summary>
    Translate,

    /// <summary>Turn (rings).</summary>
    Rotate,

    /// <summary>Resize (stalks ending in cubes).</summary>
    Scale,
}
