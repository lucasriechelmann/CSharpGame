using System.Numerics;

namespace Engine.Rendering;

/// <summary>
/// Component that makes an entity draw a textured quad at its <c>Transform</c>. Entities need both a
/// <c>Transform</c> and a <see cref="Sprite"/> to be rendered by <see cref="SpriteRenderSystem"/>.
/// </summary>
/// <remarks>
/// The texture is a direct reference for now; phase 4 replaces it with an asset handle so scenes can serialize it.
/// Prefer <see cref="Create"/> over <c>default</c> so the pivot and tint start sensible.
/// </remarks>
public struct Sprite
{
    /// <summary>The texture to draw. A sprite with no texture is skipped.</summary>
    public Texture2D? Texture;

    /// <summary>Quad size in world units, before the entity's Transform scale is applied.</summary>
    public Vector2 Size;

    /// <summary>Region of the texture to draw, in pixels (top-left origin). <see cref="Rect.Empty"/> means the whole texture.</summary>
    public Rect Source;

    /// <summary>Tint multiplied with the texture; <see cref="Color.White"/> leaves it unchanged.</summary>
    public Color Color;

    /// <summary>Pivot as a fraction of the size from the top-left; the Transform position and rotation act on this point.</summary>
    public Vector2 Pivot;

    /// <summary>Mirroring of the texture.</summary>
    public SpriteFlip Flip;

    /// <summary>Creates a white, centre-pivoted sprite showing the whole texture.</summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="size">Quad size in world units.</param>
    public static Sprite Create(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        Size = size,
        Color = Color.White,
        Pivot = new Vector2(0.5f, 0.5f),
    };
}
