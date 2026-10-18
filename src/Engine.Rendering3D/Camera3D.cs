using System.Numerics;
using Engine.Core;

namespace Engine.Rendering3D;

/// <summary>
/// A perspective camera. World space is right-handed with Y up; the camera looks down its local -Z axis (so with no rotation it
/// looks toward -Z, like OpenGL's eye space). Matrices use the row-vector convention of <see cref="Matrix4x4"/> and are uploaded
/// to OpenGL without transposing, the same as <c>Camera2D</c>.
/// </summary>
public sealed class Camera3D
{
    private float _fovY = MathF.PI / 3f;
    private float _near = 0.1f;
    private float _far = 200f;

    /// <summary>World-space position of the eye.</summary>
    public Vector3 Position { get; set; } = new(0f, 0f, 5f);

    /// <summary>Orientation: rotates the default view direction (-Z, with +Y up) into the world. Must be normalised.</summary>
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    /// <summary>Vertical field of view in radians (default 60 degrees). Between 0 and pi, exclusive.</summary>
    public float FovY
    {
        get => _fovY;
        set => _fovY = value > 0f && value < MathF.PI
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), $"Field of view must be between 0 and pi radians, got {value}.");
    }

    /// <summary>Distance of the near clip plane. Positive and smaller than <see cref="Far"/>; keep it as large as the scene allows, because depth precision is spent near it.</summary>
    public float Near
    {
        get => _near;
        set => _near = value > 0f && value < _far
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), $"Near plane must be positive and below the far plane ({_far}), got {value}.");
    }

    /// <summary>Distance of the far clip plane; anything beyond is not drawn.</summary>
    public float Far
    {
        get => _far;
        set => _far = value > _near
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), $"Far plane must be beyond the near plane ({_near}), got {value}.");
    }

    /// <summary>Size of the render target in pixels; sets the aspect ratio. Update this when the window is resized.</summary>
    public Vector2 ViewportSize { get; set; } = new(1280, 720);

    /// <summary>Width divided by height of <see cref="ViewportSize"/>.</summary>
    public float AspectRatio => ViewportSize.Y > 0f ? ViewportSize.X / ViewportSize.Y : 1f;

    /// <summary>The direction the camera looks in (unit length).</summary>
    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Rotation);

    /// <summary>The camera's right-hand direction (unit length).</summary>
    public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);

    /// <summary>The camera's up direction (unit length).</summary>
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);

    /// <summary>Turns the camera on the spot so it looks at <paramref name="target"/> with world Y as up.</summary>
    /// <exception cref="ArgumentException">The target is at the camera's position, or straight above or below it (no unique "up").</exception>
    public void LookAt(Vector3 target)
    {
        var forward = target - Position;
        if (forward.LengthSquared() < 1e-12f) throw new ArgumentException("Cannot look at the camera's own position.", nameof(target));
        forward = Vector3.Normalize(forward);
        var right = Vector3.Cross(forward, Vector3.UnitY);
        if (right.LengthSquared() < 1e-10f) throw new ArgumentException("Cannot look straight up or down: the view has no unique 'up'.", nameof(target));
        right = Vector3.Normalize(right);
        var up = Vector3.Cross(right, forward);

        // The columns of the rotation are the camera's local axes in world space: +X right, +Y up, +Z backwards.
        var basis = new Matrix4x4(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            -forward.X, -forward.Y, -forward.Z, 0f,
            0f, 0f, 0f, 1f);
        Rotation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(basis));
    }

    /// <summary>World-to-eye matrix: moves the world so the camera sits at the origin looking down -Z.</summary>
    public Matrix4x4 GetView() => Matrix4x4.CreateLookTo(Position, Forward, Up);

    /// <summary>Eye-to-clip matrix with OpenGL's depth convention (see <see cref="Projection3D.Perspective"/>).</summary>
    public Matrix4x4 GetProjection() => Projection3D.Perspective(_fovY, AspectRatio, _near, _far);

    /// <summary>World-to-clip matrix (view then projection), ready to upload to OpenGL without transposing.</summary>
    public Matrix4x4 GetViewProjection() => GetView() * GetProjection();

    /// <summary>The six planes bounding what the camera can see, for culling.</summary>
    public Frustum GetFrustum() => Frustum.FromViewProjection(GetViewProjection());

    /// <summary>The ray from the eye through a screen pixel (top-left origin), for mouse picking.</summary>
    public Ray ScreenPointToRay(Vector2 screen)
    {
        if (!Matrix4x4.Invert(GetViewProjection(), out var inverse))
            throw new InvalidOperationException("Camera matrix is not invertible (is the viewport empty?).");

        float x = 2f * screen.X / ViewportSize.X - 1f;
        float y = 1f - 2f * screen.Y / ViewportSize.Y;
        var near = Unproject(new Vector4(x, y, -1f, 1f), inverse);
        var far = Unproject(new Vector4(x, y, 1f, 1f), inverse);
        return new Ray(near, Vector3.Normalize(far - near));
    }

    /// <summary>Converts a world-space point to a screen pixel (top-left origin); false when the point is behind the camera.</summary>
    public bool TryWorldToScreen(Vector3 world, out Vector2 screen)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), GetViewProjection());
        if (clip.W <= 0f)
        {
            screen = default;
            return false;
        }

        screen = new Vector2(
            (clip.X / clip.W + 1f) * 0.5f * ViewportSize.X,
            (1f - clip.Y / clip.W) * 0.5f * ViewportSize.Y);
        return true;
    }

    private static Vector3 Unproject(Vector4 clip, in Matrix4x4 inverse)
    {
        var world = Vector4.Transform(clip, inverse);
        return new Vector3(world.X, world.Y, world.Z) / world.W;
    }
}
