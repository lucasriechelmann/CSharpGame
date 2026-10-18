namespace Arena3D;

/// <summary>Makes an entity bob up and down and turn about the vertical axis, around the height it started at. Plain data; <see cref="OrbBobSystem"/> animates it.</summary>
public struct OrbBob
{
    /// <summary>Metres the entity rises and falls from its resting height.</summary>
    public float Amplitude;

    /// <summary>Bobbing speed in radians per second.</summary>
    public float Speed;

    /// <summary>Turning speed in radians per second.</summary>
    public float Spin;

    /// <summary>Offset in radians, so a row of orbs does not move in step.</summary>
    public float Phase;

    /// <summary>The resting height, remembered the first time the system sees the entity (so the scene file only needs the position).</summary>
    public float RestHeight;

    /// <summary>True once <see cref="RestHeight"/> has been recorded.</summary>
    public bool Started;
}
