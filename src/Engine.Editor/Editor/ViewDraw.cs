using System.Numerics;
using ImGuiNET;

namespace Engine.Editor;

/// <summary>
/// An ImGui draw list that takes points in the scene view's own coordinates (0,0 at the view's top-left corner, like the game's camera
/// and the mouse position the editor works with) and shifts them to where the view sits in the window. Selection outlines, the gizmo and
/// the tile cursor draw through it, so they do not need to know the view is a docked panel.
/// </summary>
internal readonly struct ViewDraw(ImDrawListPtr list, Vector2 origin)
{
    /// <summary>Draws a line.</summary>
    public void AddLine(Vector2 a, Vector2 b, uint colour, float thickness) => list.AddLine(a + origin, b + origin, colour, thickness);

    /// <summary>Draws a filled rectangle.</summary>
    public void AddRectFilled(Vector2 min, Vector2 max, uint colour) => list.AddRectFilled(min + origin, max + origin, colour);

    /// <summary>Draws a rectangle outline.</summary>
    public void AddRect(Vector2 min, Vector2 max, uint colour, float thickness) => list.AddRect(min + origin, max + origin, colour, 0f, ImDrawFlags.None, thickness);

    /// <summary>Draws a quadrilateral outline.</summary>
    public void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, uint colour, float thickness) =>
        list.AddQuad(a + origin, b + origin, c + origin, d + origin, colour, thickness);

    /// <summary>Draws a circle outline.</summary>
    public void AddCircle(Vector2 centre, float radius, uint colour, int segments, float thickness) =>
        list.AddCircle(centre + origin, radius, colour, segments, thickness);

    /// <summary>Draws a filled triangle.</summary>
    public void AddTriangleFilled(Vector2 a, Vector2 b, Vector2 c, uint colour) => list.AddTriangleFilled(a + origin, b + origin, c + origin, colour);
}
