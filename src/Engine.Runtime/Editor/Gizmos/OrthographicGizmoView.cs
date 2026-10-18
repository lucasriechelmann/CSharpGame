using System.Numerics;
using Engine.Core;
using Engine.Rendering;

namespace Engine.Runtime.Editor.Gizmos;

/// <summary>The gizmo's view of a <see cref="Camera2D"/>: rays run straight down the Z axis, one world unit per <c>1 / Zoom</c> pixels.</summary>
/// <param name="camera">The 2D scene camera.</param>
public sealed class OrthographicGizmoView(Camera2D camera) : IGizmoView
{
    /// <summary>How far above the scene a 2D ray starts; everything in a 2D scene lies well below it.</summary>
    private const float RayHeight = 1000f;

    /// <inheritdoc />
    public bool IsOrthographic2D => true;

    /// <inheritdoc />
    public Vector3 ViewForward => -Vector3.UnitZ;

    /// <inheritdoc />
    public Ray ScreenRay(Vector2 screen)
    {
        var world = camera.ScreenToWorld(screen);
        return new Ray(new Vector3(world.X, world.Y, RayHeight), -Vector3.UnitZ);
    }

    /// <inheritdoc />
    public bool TryProject(Vector3 world, out Vector2 screen)
    {
        screen = camera.WorldToScreen(new Vector2(world.X, world.Y));
        return true;
    }

    /// <inheritdoc />
    public float WorldUnitsPerPixel(Vector3 at) => 1f / camera.Zoom;
}
