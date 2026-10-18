namespace Engine.Rendering;

/// <summary>An RGBA colour with float components in the 0..1 range. Laid out as four floats so it can be sent to the GPU as-is.</summary>
/// <param name="R">Red, 0..1.</param>
/// <param name="G">Green, 0..1.</param>
/// <param name="B">Blue, 0..1.</param>
/// <param name="A">Alpha (opacity), 0..1.</param>
public readonly record struct Color(float R, float G, float B, float A = 1f)
{
    /// <summary>Opaque white. Multiplying a texture by white leaves it unchanged.</summary>
    public static Color White => new(1f, 1f, 1f);

    /// <summary>Opaque black.</summary>
    public static Color Black => new(0f, 0f, 0f);

    /// <summary>Opaque red.</summary>
    public static Color Red => new(1f, 0f, 0f);

    /// <summary>Opaque green.</summary>
    public static Color Green => new(0f, 1f, 0f);

    /// <summary>Opaque blue.</summary>
    public static Color Blue => new(0f, 0f, 1f);

    /// <summary>Fully transparent.</summary>
    public static Color Transparent => new(0f, 0f, 0f, 0f);
}
