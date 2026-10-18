using System.Numerics;

namespace Platformer;

/// <summary>World-space position. Stored as Vector3 (Z = depth/layer) so the same type serves a future 3D renderer.</summary>
public struct Position
{
    /// <summary>The position in world units.</summary>
    public Vector3 Value;
}
