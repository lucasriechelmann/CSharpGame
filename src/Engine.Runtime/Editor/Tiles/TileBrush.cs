namespace Engine.Runtime.Editor.Tiles;

/// <summary>How the paint tool turns mouse input into cells.</summary>
public enum TileBrush
{
    /// <summary>Paints the cells the mouse passes over while the button is held.</summary>
    Pencil,

    /// <summary>Drag out a rectangle and release to fill it.</summary>
    Rectangle,

    /// <summary>Click to fill the connected area of equal tiles (inside the visible part of the layer).</summary>
    Fill,
}
