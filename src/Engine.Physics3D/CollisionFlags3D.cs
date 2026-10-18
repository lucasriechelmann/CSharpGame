namespace Engine.Physics3D;

/// <summary>Which sides of a body touched something solid during the last physics step.</summary>
[Flags]
public enum CollisionFlags3D
{
    /// <summary>No contact.</summary>
    None = 0,

    /// <summary>Standing on something (-Y side).</summary>
    Below = 1,

    /// <summary>Head against something (+Y side).</summary>
    Above = 2,

    /// <summary>Touching something on the -X side.</summary>
    MinX = 4,

    /// <summary>Touching something on the +X side.</summary>
    MaxX = 8,

    /// <summary>Touching something on the -Z side.</summary>
    MinZ = 16,

    /// <summary>Touching something on the +Z side.</summary>
    MaxZ = 32,
}
