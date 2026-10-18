namespace Engine.Rendering3D;

/// <summary>How an animation channel fills in the time between two keyframes.</summary>
public enum AnimationInterpolation
{
    /// <summary>A straight blend between the two values (rotations take the shortest arc).</summary>
    Linear,

    /// <summary>Holds the earlier value until the next key arrives.</summary>
    Step,

    /// <summary>A smooth Hermite curve using a tangent stored with each key (three values per key: in-tangent, value, out-tangent).</summary>
    CubicSpline,
}
