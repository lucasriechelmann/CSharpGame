namespace Engine.Physics3D;

/// <summary>A coordinate axis. Collision is resolved one axis at a time, so a body slides along walls and floors.</summary>
public enum Axis3D
{
    /// <summary>Left-right.</summary>
    X,

    /// <summary>Up is positive.</summary>
    Y,

    /// <summary>Toward the viewer is positive (the default camera looks down -Z).</summary>
    Z,
}
