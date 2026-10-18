namespace Arena3D;

/// <summary>A pick-up: the player collects it by coming within <see cref="Radius"/> metres of its centre. Plain data; <see cref="OrbCollectSystem"/> does the detecting.</summary>
public struct Orb
{
    /// <summary>How close, in metres, the player's centre must come to collect the orb.</summary>
    public float Radius;
}
