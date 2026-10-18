using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// Position, rotation and scale of an entity in world space. Fully 3D (Vector3/Quaternion) so the same
/// component works for a future 3D renderer; 2D code uses X/Y for placement, Z for draw order, and
/// <see cref="RotationZ"/> for rotation.
/// </summary>
/// <remarks>
/// <c>default(Transform)</c> has a zero scale and an invalid quaternion. Always start from
/// <see cref="Identity"/> or <see cref="At(Vector3)"/>.
/// </remarks>
public struct Transform
{
    /// <summary>World-space position. In 2D, a larger Z draws in front.</summary>
    public Vector3 Position;

    /// <summary>World-space orientation.</summary>
    public Quaternion Rotation;

    /// <summary>Per-axis scale multiplier.</summary>
    public Vector3 Scale;

    /// <summary>No translation, no rotation, unit scale.</summary>
    public static Transform Identity => new() { Position = Vector3.Zero, Rotation = Quaternion.Identity, Scale = Vector3.One };

    /// <summary>An unrotated, unscaled transform at <paramref name="position"/>.</summary>
    public static Transform At(Vector3 position) => new() { Position = position, Rotation = Quaternion.Identity, Scale = Vector3.One };

    /// <summary>An unrotated, unscaled transform at the 2D point (<paramref name="x"/>, <paramref name="y"/>) on layer <paramref name="z"/>.</summary>
    public static Transform At(float x, float y, float z = 0f) => At(new Vector3(x, y, z));

    /// <summary>The rotation around the Z axis in radians (counter-clockwise). Meaningful for 2D, where Z is the only rotation axis.</summary>
    public readonly float RotationZ =>
        MathF.Atan2(
            2f * (Rotation.W * Rotation.Z + Rotation.X * Rotation.Y),
            1f - 2f * (Rotation.Y * Rotation.Y + Rotation.Z * Rotation.Z));

    /// <summary>Sets the rotation to a pure rotation around the Z axis.</summary>
    /// <param name="radians">Counter-clockwise angle in radians.</param>
    public void SetRotationZ(float radians) => Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, radians);
}
