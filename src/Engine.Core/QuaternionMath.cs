using System.Numerics;

namespace Engine.Core;

/// <summary>
/// Conversions between quaternions and Euler angles in degrees, for tools that show rotation to a human. Components keep the
/// quaternion (no gimbal lock, composes cleanly); Euler angles exist only at the editing boundary.
/// </summary>
public static class QuaternionMath
{
    private const float RadToDeg = 180f / MathF.PI;
    private const float DegToRad = MathF.PI / 180f;

    /// <summary>
    /// Builds a rotation from degrees about X (pitch), Y (yaw) and Z (roll), the order <see cref="Quaternion.CreateFromYawPitchRoll"/>
    /// uses. In 2D only Z (roll) matters.
    /// </summary>
    public static Quaternion FromEulerDegrees(Vector3 degrees) =>
        Quaternion.CreateFromYawPitchRoll(degrees.Y * DegToRad, degrees.X * DegToRad, degrees.Z * DegToRad);

    /// <summary>
    /// Recovers (pitch, yaw, roll) in degrees from a rotation. Pitch is limited to -90..90; at exactly ±90 the axes line up
    /// (gimbal lock) and yaw and roll can no longer be told apart, so only their combination round-trips there.
    /// </summary>
    public static Vector3 ToEulerDegrees(Quaternion q)
    {
        float sinPitch = Math.Clamp(2f * (q.W * q.X - q.Y * q.Z), -1f, 1f);
        float pitch = MathF.Asin(sinPitch);
        float yaw = MathF.Atan2(2f * (q.X * q.Z + q.W * q.Y), 1f - 2f * (q.X * q.X + q.Y * q.Y));
        float roll = MathF.Atan2(2f * (q.X * q.Y + q.W * q.Z), 1f - 2f * (q.X * q.X + q.Z * q.Z));
        return new Vector3(pitch, yaw, roll) * RadToDeg;
    }
}
