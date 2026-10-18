using System.Numerics;
using Engine.Core;
using Engine.Rendering3D;

namespace Engine.Runtime.Editor.Gizmos;

/// <summary>The gizmo's view of a <see cref="Camera3D"/>.</summary>
/// <param name="camera">The 3D scene camera.</param>
public sealed class PerspectiveGizmoView(Camera3D camera) : IGizmoView
{
    /// <inheritdoc />
    public bool IsOrthographic2D => false;

    /// <inheritdoc />
    public Vector3 ViewForward => camera.Forward;

    /// <inheritdoc />
    public Ray ScreenRay(Vector2 screen) => camera.ScreenPointToRay(screen);

    /// <inheritdoc />
    public bool TryProject(Vector3 world, out Vector2 screen) => camera.TryWorldToScreen(world, out screen);

    /// <inheritdoc />
    public float WorldUnitsPerPixel(Vector3 at) => EntityPicking3D.WorldUnitsPerPixel(camera, at);
}
