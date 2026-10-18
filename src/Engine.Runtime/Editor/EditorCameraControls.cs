using System.Numerics;
using Engine.Rendering;

namespace Engine.Runtime.Editor;

/// <summary>Pan and zoom for the editor's view of the scene, as pure functions on a <see cref="Camera2D"/>.</summary>
public static class EditorCameraControls
{
    /// <summary>Smallest zoom the editor allows (zoomed far out).</summary>
    public const float MinZoom = 0.05f;

    /// <summary>Largest zoom the editor allows (zoomed far in).</summary>
    public const float MaxZoom = 16f;

    /// <summary>Zoom multiplier per scroll tick.</summary>
    public const float ZoomStep = 1.1f;

    /// <summary>
    /// Drags the view so the world follows the mouse: the point under the cursor stays under it.
    /// </summary>
    /// <param name="camera">The camera to move.</param>
    /// <param name="screenDelta">How far the mouse moved this frame, in screen pixels (Y down).</param>
    public static void Pan(Camera2D camera, Vector2 screenDelta) =>
        camera.Position -= new Vector2(screenDelta.X, -screenDelta.Y) / camera.Zoom;

    /// <summary>
    /// Zooms in or out around a screen point, keeping the world point under that pixel where it is (so the view zooms towards the cursor).
    /// </summary>
    /// <param name="camera">The camera to zoom.</param>
    /// <param name="screenPoint">The pixel to zoom around, usually the mouse.</param>
    /// <param name="scrollTicks">Wheel ticks; positive zooms in.</param>
    public static void ZoomAt(Camera2D camera, Vector2 screenPoint, float scrollTicks)
    {
        if (scrollTicks == 0f) return;

        var before = camera.ScreenToWorld(screenPoint);
        camera.Zoom = Math.Clamp(camera.Zoom * MathF.Pow(ZoomStep, scrollTicks), MinZoom, MaxZoom);
        var after = camera.ScreenToWorld(screenPoint);
        camera.Position += before - after;
    }
}
