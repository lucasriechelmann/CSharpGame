using System.Numerics;

namespace Engine.Rendering;

/// <summary>
/// Where a low-resolution game image sits inside the window in pixel-perfect mode: scaled up by the largest whole number that fits (so
/// every game pixel becomes a crisp square of the same size) and centred, with black bars around it. Pure maths, no GL.
/// </summary>
/// <param name="Scale">Window pixels per game pixel. A whole number when the window is at least as big as the game image; smaller than 1 when it must shrink to fit.</param>
/// <param name="X">Left edge of the image in window pixels.</param>
/// <param name="Y">Distance of the image's top (and bottom) edge from the window's top (and bottom), in window pixels.</param>
/// <param name="Width">Width of the drawn image in window pixels.</param>
/// <param name="Height">Height of the drawn image in window pixels.</param>
public readonly record struct PixelPerfectLayout(float Scale, int X, int Y, int Width, int Height)
{
    /// <summary>Works out the layout.</summary>
    /// <param name="virtualWidth">Width of the game image in game pixels.</param>
    /// <param name="virtualHeight">Height of the game image in game pixels.</param>
    /// <param name="windowWidth">Window width in pixels.</param>
    /// <param name="windowHeight">Window height in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size is not positive.</exception>
    public static PixelPerfectLayout Compute(int virtualWidth, int virtualHeight, int windowWidth, int windowHeight)
    {
        if (virtualWidth <= 0 || virtualHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(virtualWidth), $"The game image must have a positive size, got {virtualWidth}x{virtualHeight}.");
        if (windowWidth <= 0 || windowHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(windowWidth), $"The window must have a positive size, got {windowWidth}x{windowHeight}.");

        int whole = Math.Min(windowWidth / virtualWidth, windowHeight / virtualHeight);
        float scale = whole >= 1 ? whole : Math.Min((float)windowWidth / virtualWidth, (float)windowHeight / virtualHeight);
        int width = (int)MathF.Round(virtualWidth * scale);
        int height = (int)MathF.Round(virtualHeight * scale);
        return new PixelPerfectLayout(scale, (windowWidth - width) / 2, (windowHeight - height) / 2, width, height);
    }

    /// <summary>Converts a window position (pixels, origin top-left, such as the mouse) to a game-image position; outside the image the result is outside <c>0..size</c>.</summary>
    /// <param name="window">A position in the window.</param>
    public Vector2 WindowToGame(Vector2 window) => new((window.X - X) / Scale, (window.Y - Y) / Scale);

    /// <summary>Converts a game-image position (pixels, origin top-left) to a window position.</summary>
    /// <param name="game">A position in the game image.</param>
    public Vector2 GameToWindow(Vector2 game) => new(game.X * Scale + X, game.Y * Scale + Y);
}
