using System.Numerics;

namespace Engine.Rendering;

/// <summary>
/// An orthographic 2D camera. With <see cref="Zoom"/> = 1, one world unit is one screen pixel.
/// World Y points up; screen coordinates have their origin at the top-left with Y pointing down.
/// </summary>
public sealed class Camera2D
{
    private float _zoom = 1f;

    /// <summary>World-space point at the centre of the screen.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Counter-clockwise rotation of the view, in radians.</summary>
    public float Rotation { get; set; }

    /// <summary>Pixels per world unit. Greater than 1 zooms in. Must be positive.</summary>
    public float Zoom
    {
        get => _zoom;
        set => _zoom = value > 0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Zoom must be positive.");
    }

    /// <summary>Size of the render target in pixels. Update this when the window is resized.</summary>
    public Vector2 ViewportSize { get; set; } = new(1280, 720);

    /// <summary>
    /// World-to-clip matrix (row-vector convention, ready to upload to OpenGL without transposing).
    /// </summary>
    public Matrix4x4 GetViewProjection()
    {
        // World -> view: undo the camera's translation and rotation, then scale by zoom.
        var view = Matrix4x4.CreateTranslation(-Position.X, -Position.Y, 0f)
                 * Matrix4x4.CreateRotationZ(-Rotation)
                 * Matrix4x4.CreateScale(_zoom, _zoom, 1f);

        // View -> clip: pixels centred on the screen, Y up. Z range is generous because 2D uses Z only for ordering.
        float halfWidth = ViewportSize.X / 2f;
        float halfHeight = ViewportSize.Y / 2f;
        var projection = Matrix4x4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, -1000f, 1000f);

        return view * projection;
    }

    /// <summary>Converts a screen pixel (top-left origin) to a world-space point.</summary>
    public Vector2 ScreenToWorld(Vector2 screen)
    {
        if (!Matrix4x4.Invert(GetViewProjection(), out var inverse))
            throw new InvalidOperationException("Camera matrix is not invertible (is the viewport empty?).");

        var ndc = new Vector3(
            2f * screen.X / ViewportSize.X - 1f,
            1f - 2f * screen.Y / ViewportSize.Y,
            0f);
        var world = Vector3.Transform(ndc, inverse);
        return new Vector2(world.X, world.Y);
    }

    /// <summary>Converts a world-space point to a screen pixel (top-left origin).</summary>
    public Vector2 WorldToScreen(Vector2 world)
    {
        var clip = Vector3.Transform(new Vector3(world, 0f), GetViewProjection());
        return new Vector2(
            (clip.X + 1f) * 0.5f * ViewportSize.X,
            (1f - clip.Y) * 0.5f * ViewportSize.Y);
    }
}
