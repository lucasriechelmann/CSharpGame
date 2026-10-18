using System.Numerics;

namespace Engine.Rendering;

/// <summary>Everything the batch needs to draw one sprite quad. Pure data; the texture is passed separately.</summary>
public struct SpriteDraw
{
    /// <summary>World-space position of the pivot point. Z is draw order (larger = in front).</summary>
    public Vector3 Position;

    /// <summary>Size of the quad in world units.</summary>
    public Vector2 Size;

    /// <summary>Pivot as a fraction of the size, measured from the top-left: (0.5, 0.5) is the centre, (0.5, 1) the bottom-middle.</summary>
    public Vector2 Pivot;

    /// <summary>Counter-clockwise rotation around the pivot, in radians.</summary>
    public float Rotation;

    /// <summary>Region of the texture to draw, in normalised 0..1 coordinates.</summary>
    public Rect UvRect;

    /// <summary>Tint multiplied with the texture.</summary>
    public Color Color;

    /// <summary>Mirroring of the texture coordinates.</summary>
    public SpriteFlip Flip;

    /// <summary>A white, unrotated, centre-pivoted draw of the whole texture, at the given position and size.</summary>
    public static SpriteDraw At(Vector3 position, Vector2 size) => new()
    {
        Position = position,
        Size = size,
        Pivot = new Vector2(0.5f, 0.5f),
        UvRect = Rect.UnitSquare,
        Color = Color.White,
    };
}
