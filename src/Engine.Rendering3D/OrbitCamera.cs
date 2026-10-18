using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// The maths of an "editor style" orbit camera: it circles a target point at a distance, with yaw and pitch angles (so the view never
/// rolls). Dragging orbits, panning slides the target, the wheel changes the distance, and focusing re-centres on an object. Pure, like
/// <see cref="FlyCamera"/>; the editor feeds it mouse deltas and copies the result to a <see cref="Camera3D"/>.
/// </summary>
public sealed class OrbitCamera
{
    /// <summary>Pitch stops just short of +-90 degrees, where yaw becomes ambiguous.</summary>
    public const float MaxPitch = 89f * MathF.PI / 180f;

    /// <summary>The closest the eye may get to the target.</summary>
    public const float MinDistance = 0.1f;

    /// <summary>The farthest the eye may get from the target.</summary>
    public const float MaxDistance = 1000f;

    /// <summary>Distance multiplier per wheel tick (zooming in divides by it).</summary>
    public const float ZoomStep = 1.1f;

    private float _pitch;
    private float _distance = 8f;

    /// <summary>The point the camera looks at and circles.</summary>
    public Vector3 Target { get; set; }

    /// <summary>Rotation about world Y in radians; the same convention as <see cref="FlyCamera.Yaw"/>.</summary>
    public float Yaw { get; set; }

    /// <summary>Tilt in radians, clamped to +-<see cref="MaxPitch"/>. Positive looks up (so the eye sits below the target).</summary>
    public float Pitch
    {
        get => _pitch;
        set => _pitch = Math.Clamp(value, -MaxPitch, MaxPitch);
    }

    /// <summary>Distance from the eye to the target, clamped to <see cref="MinDistance"/> .. <see cref="MaxDistance"/>.</summary>
    public float Distance
    {
        get => _distance;
        set => _distance = Math.Clamp(value, MinDistance, MaxDistance);
    }

    /// <summary>The orientation for the current yaw and pitch.</summary>
    public Quaternion Rotation => Quaternion.CreateFromYawPitchRoll(Yaw, _pitch, 0f);

    /// <summary>The direction the camera looks in.</summary>
    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Rotation);

    /// <summary>The eye position: the target pulled back along the view direction.</summary>
    public Vector3 Position => Target - Forward * _distance;

    /// <summary>
    /// Starts from an existing camera: the target is the point <paramref name="distance"/> in front of it, and the angles reproduce its
    /// view direction, so the first frame of the editor looks exactly like the game did.
    /// </summary>
    /// <param name="camera">The camera to copy.</param>
    /// <param name="distance">How far in front of the camera the orbit centre should be.</param>
    public static OrbitCamera From(Camera3D camera, float distance)
    {
        var forward = camera.Forward;
        var orbit = new OrbitCamera
        {
            Yaw = MathF.Atan2(-forward.X, -forward.Z),
            Pitch = MathF.Asin(Math.Clamp(forward.Y, -1f, 1f)),
            Distance = distance,
        };
        orbit.Target = camera.Position + forward * orbit.Distance;
        return orbit;
    }

    /// <summary>Turns the camera around the target by the given amounts in radians.</summary>
    /// <param name="yawDelta">Added to <see cref="Yaw"/>.</param>
    /// <param name="pitchDelta">Added to <see cref="Pitch"/>.</param>
    public void Orbit(float yawDelta, float pitchDelta)
    {
        Yaw += yawDelta;
        Pitch += pitchDelta;
    }

    /// <summary>
    /// Slides the target (and so the eye) in the screen plane so the world follows the mouse: a point at the target's depth stays under
    /// the cursor.
    /// </summary>
    /// <param name="screenDelta">How far the mouse moved, in pixels (Y down).</param>
    /// <param name="fovY">Vertical field of view of the camera, in radians.</param>
    /// <param name="viewportHeight">Height of the view in pixels.</param>
    public void Pan(Vector2 screenDelta, float fovY, float viewportHeight)
    {
        if (viewportHeight <= 0f) return;

        float worldPerPixel = 2f * _distance * MathF.Tan(fovY * 0.5f) / viewportHeight;
        var rotation = Rotation;
        var right = Vector3.Transform(Vector3.UnitX, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        Target += (-right * screenDelta.X + up * screenDelta.Y) * worldPerPixel;
    }

    /// <summary>Moves the eye towards or away from the target.</summary>
    /// <param name="scrollTicks">Wheel ticks; positive zooms in.</param>
    public void Zoom(float scrollTicks)
    {
        if (scrollTicks == 0f) return;
        Distance = _distance / MathF.Pow(ZoomStep, scrollTicks);
    }

    /// <summary>Re-centres on a sphere and backs off until the whole sphere fits in view (with a little margin), keeping the viewing angle.</summary>
    /// <param name="centre">The centre of the thing to look at.</param>
    /// <param name="radius">Its bounding radius; a tiny or zero radius still keeps a sensible distance.</param>
    /// <param name="fovY">Vertical field of view of the camera, in radians.</param>
    public void FocusOn(Vector3 centre, float radius, float fovY)
    {
        Target = centre;
        Distance = MathF.Max(radius, 0.25f) / MathF.Sin(fovY * 0.5f) * 1.25f;
    }

    /// <summary>Writes the position and orientation to a camera.</summary>
    /// <param name="camera">The camera to drive.</param>
    public void ApplyTo(Camera3D camera)
    {
        camera.Position = Position;
        camera.Rotation = Rotation;
    }
}
