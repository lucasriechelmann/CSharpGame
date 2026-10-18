namespace Engine.Editor.Gizmos;

/// <summary>Which axes the gizmo's handles follow.</summary>
public enum GizmoSpace
{
    /// <summary>The world's X, Y and Z, whatever the entity's orientation.</summary>
    World,

    /// <summary>The entity's own axes, turned with it. Scaling always uses these, because scale lives on the entity's own axes.</summary>
    Local,
}
