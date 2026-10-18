using System.Numerics;

namespace Engine.Editor;

/// <summary>Where the scene view panel is this frame, and whether the mouse is over it. Everything the editor does with the mouse in the scene is relative to <see cref="Origin"/>.</summary>
internal sealed class SceneViewport
{
    /// <summary>Window position of the view's top-left corner.</summary>
    public Vector2 Origin { get; private set; }

    /// <summary>Size of the view in pixels.</summary>
    public Vector2 Size { get; private set; }

    /// <summary>True while the mouse is over the view and nothing in front of it (a menu, a popup) has taken the mouse.</summary>
    public bool Hovered { get; private set; }

    /// <summary>Records this frame's placement.</summary>
    public void Update(Vector2 origin, Vector2 size, bool hovered)
    {
        Origin = origin;
        Size = size;
        Hovered = hovered;
    }

    /// <summary>Turns a window position (such as the mouse) into a position in the view.</summary>
    public Vector2 ToView(Vector2 window) => window - Origin;
}
