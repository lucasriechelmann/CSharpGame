using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// The maths of a free-flying "editor style" camera: yaw and pitch angles (so the view never rolls) plus a position, applied to a
/// <see cref="Camera3D"/>. Kept apart from input so it is testable and any game can feed it from the mouse, a gamepad or a script.
/// </summary>
public sealed class FlyCamera
{
    /// <summary>Looking straight up or down makes yaw ambiguous (gimbal), so pitch stops just short of +-90 degrees.</summary>
    public const float MaxPitch = 89f * MathF.PI / 180f;

    private float _pitch;

    /// <summary>World-space position of the eye.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Rotation about world Y in radians. 0 looks toward -Z; positive turns left (counter-clockwise seen from above).</summary>
    public float Yaw { get; set; }

    /// <summary>Tilt in radians, clamped to +-<see cref="MaxPitch"/>. Positive looks up.</summary>
    public float Pitch
    {
        get => _pitch;
        set => _pitch = Math.Clamp(value, -MaxPitch, MaxPitch);
    }

    /// <summary>The orientation for the current yaw and pitch (never rolled).</summary>
    public Quaternion Rotation => Quaternion.CreateFromYawPitchRoll(Yaw, _pitch, 0f);

    /// <summary>Starts from an existing camera: copies its position and works out the angles that reproduce its view direction.</summary>
    public static FlyCamera From(Camera3D camera)
    {
        var fly = new FlyCamera();
        fly.CopyFrom(camera);
        return fly;
    }

    /// <summary>
    /// Takes over a camera's current view: position, and the yaw and pitch that reproduce its view direction. Call it when something else
    /// (the scene editor's orbit camera) moved the camera, so flying on continues from where it was left instead of snapping back.
    /// </summary>
    /// <param name="camera">The camera to copy.</param>
    public void CopyFrom(Camera3D camera)
    {
        var forward = camera.Forward;
        Position = camera.Position;
        Yaw = MathF.Atan2(-forward.X, -forward.Z);
        Pitch = MathF.Asin(Math.Clamp(forward.Y, -1f, 1f));
    }

    /// <summary>Turns the view by the given amounts in radians.</summary>
    /// <param name="yawDelta">Added to <see cref="Yaw"/> (positive turns left).</param>
    /// <param name="pitchDelta">Added to <see cref="Pitch"/> (positive looks up).</param>
    public void Look(float yawDelta, float pitchDelta)
    {
        Yaw += yawDelta;
        Pitch += pitchDelta;
    }

    /// <summary>
    /// Moves the eye. Forward and strafe follow the view direction (flying where you look); up is along world Y so it does not
    /// depend on where you look.
    /// </summary>
    /// <param name="forward">-1..1: back to forward.</param>
    /// <param name="right">-1..1: left to right.</param>
    /// <param name="up">-1..1: down to up.</param>
    /// <param name="distance">How far full input moves the eye in this call (speed times time step).</param>
    public void Move(float forward, float right, float up, float distance)
    {
        var rotation = Rotation;
        var direction = Vector3.Transform(-Vector3.UnitZ, rotation) * forward
                      + Vector3.Transform(Vector3.UnitX, rotation) * right
                      + Vector3.UnitY * up;
        // Normalise only when over unit length, so diagonal movement is not faster but gentle analog input stays gentle.
        if (direction.LengthSquared() > 1f) direction = Vector3.Normalize(direction);
        Position += direction * distance;
    }

    /// <summary>Writes the position and orientation to a camera.</summary>
    public void ApplyTo(Camera3D camera)
    {
        camera.Position = Position;
        camera.Rotation = Rotation;
    }
}
