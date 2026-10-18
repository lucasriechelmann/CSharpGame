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
    /// Pixel-perfect mode: when true, the camera and everything drawn through it (sprites, tile layers) is positioned on whole pixels of
    /// the camera's viewport, so pixel art does not shimmer or blur while the camera or sprites move at fractions of a pixel. Use it
    /// with a viewport equal to the render resolution (see <see cref="PostProcessChain.SetPixelPerfect"/>) and a whole-number zoom.
    /// </summary>
    public bool PixelSnap { get; set; }

    /// <summary>The camera position the view is actually built from: <see cref="Position"/>, rounded to a whole pixel when <see cref="PixelSnap"/> is on.</summary>
    public Vector2 ViewPosition => Snap(Position);

    /// <summary>Rounds a world position to the camera's pixel grid (one pixel is <c>1 / Zoom</c> world units); returns it unchanged when <see cref="PixelSnap"/> is off.</summary>
    /// <param name="world">A world position.</param>
    public Vector2 Snap(Vector2 world) =>
        PixelSnap ? new Vector2(MathF.Round(world.X * _zoom) / _zoom, MathF.Round(world.Y * _zoom) / _zoom) : world;

    /// <summary>
    /// World-to-clip matrix (row-vector convention, ready to upload to OpenGL without transposing).
    /// </summary>
    public Matrix4x4 GetViewProjection()
    {
        // World -> view: undo the camera's translation and rotation, then scale by zoom.
        var view = Matrix4x4.CreateTranslation(-ViewPosition.X, -ViewPosition.Y, 0f)
                 * Matrix4x4.CreateRotationZ(-Rotation)
                 * Matrix4x4.CreateScale(_zoom, _zoom, 1f);

        // View -> clip: pixels centred on the screen, Y up. Z range is generous because 2D uses Z only for ordering.
        float halfWidth = ViewportSize.X / 2f;
        float halfHeight = ViewportSize.Y / 2f;
        var projection = Matrix4x4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, -1000f, 1000f);

        return view * projection;
    }

    /// <summary>
    /// The smallest axis-aligned world-space box that contains everything the camera can see (accounts for zoom and
    /// rotation). Use it to cull: skip anything that lies outside.
    /// </summary>
    /// <param name="min">Bottom-left corner of the box.</param>
    /// <param name="max">Top-right corner of the box.</param>
    public void GetVisibleBounds(out Vector2 min, out Vector2 max)
    {
        if (!Matrix4x4.Invert(GetViewProjection(), out var inverse))
            throw new InvalidOperationException("Camera matrix is not invertible (is the viewport empty?).");

        min = new Vector2(float.MaxValue);
        max = new Vector2(float.MinValue);
        for (int corner = 0; corner < 4; corner++)
        {
            // The four clip-space corners (+-1, +-1) mapped back into the world.
            var clip = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, 0f);
            var world = Vector3.Transform(clip, inverse);
            min = Vector2.Min(min, new Vector2(world.X, world.Y));
            max = Vector2.Max(max, new Vector2(world.X, world.Y));
        }
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
