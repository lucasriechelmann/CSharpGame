using System.Numerics;

namespace Scene3D;

/// <summary>Turns an entity around an axis at a constant rate (radians per second); registered as a component so scenes can set it.</summary>
public struct Spin
{
    /// <summary>The axis to turn around, in world space. Does not need to be normalised.</summary>
    public Vector3 Axis;

    /// <summary>Turn rate in radians per second.</summary>
    public float Speed;
}
