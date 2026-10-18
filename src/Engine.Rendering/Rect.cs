namespace Engine.Rendering;

/// <summary>An axis-aligned rectangle (x, y, width, height). Used for sprite-sheet regions, where (0,0) is the texture's top-left.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Horizontal extent.</param>
/// <param name="Height">Vertical extent.</param>
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    /// <summary>The zero rectangle. In a <see cref="Sprite"/> it means "use the whole texture".</summary>
    public static Rect Empty => default;

    /// <summary>The full 0..1 texture-coordinate square.</summary>
    public static Rect UnitSquare => new(0f, 0f, 1f, 1f);

    /// <summary>True when the rectangle has no area.</summary>
    public bool IsEmpty => Width <= 0f || Height <= 0f;

    /// <summary>The rectangle shrunk by <paramref name="amount"/> on every side. Used to keep atlas sampling inside a tile and avoid bleeding from neighbours.</summary>
    /// <param name="amount">Distance to move each edge inwards.</param>
    public Rect Inset(float amount) => new(X + amount, Y + amount, Width - 2f * amount, Height - 2f * amount);

    /// <summary>
    /// Converts a pixel rectangle to normalised 0..1 texture coordinates. An empty rectangle maps to the whole texture.
    /// </summary>
    /// <param name="textureWidth">Texture width in pixels.</param>
    /// <param name="textureHeight">Texture height in pixels.</param>
    public Rect ToUv(float textureWidth, float textureHeight) =>
        IsEmpty
            ? UnitSquare
            : new Rect(X / textureWidth, Y / textureHeight, Width / textureWidth, Height / textureHeight);
}
