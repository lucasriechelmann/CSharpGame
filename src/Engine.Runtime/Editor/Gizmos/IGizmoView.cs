using System.Numerics;
using Engine.Core;

namespace Engine.Runtime.Editor.Gizmos;

/// <summary>
/// What the gizmo needs to know about the camera that looks at it, so one gizmo serves the 2D editor view (an orthographic camera) and
/// the 3D one (a perspective camera). The gizmo is drawn and dragged in screen space, so it needs to turn screen points into world rays
/// and back, and to know how big a pixel is where the gizmo stands.
/// </summary>
public interface IGizmoView
{
    /// <summary>True for the 2D editor view: only the X and Y arrows and the Z rotation ring exist, because the view looks straight down Z.</summary>
    bool IsOrthographic2D { get; }

    /// <summary>The direction the camera looks in; free movement happens in the plane facing against it.</summary>
    Vector3 ViewForward { get; }

    /// <summary>The world ray through a screen pixel (top-left origin).</summary>
    /// <param name="screen">The pixel.</param>
    Ray ScreenRay(Vector2 screen);

    /// <summary>Converts a world point to a screen pixel; false when it is behind the camera.</summary>
    /// <param name="world">The point.</param>
    /// <param name="screen">The pixel.</param>
    bool TryProject(Vector3 world, out Vector2 screen);

    /// <summary>How many world units one pixel spans at <paramref name="at"/>, so the gizmo keeps the same size on screen at any zoom or distance.</summary>
    /// <param name="at">The world point (the gizmo's origin).</param>
    float WorldUnitsPerPixel(Vector3 at);
}
