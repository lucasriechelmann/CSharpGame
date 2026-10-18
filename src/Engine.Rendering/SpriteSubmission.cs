namespace Engine.Rendering;

/// <summary>A sprite gathered from the world, waiting to be sorted and sent to the <see cref="SpriteBatch"/>.</summary>
internal struct SpriteSubmission
{
    /// <summary>Texture to draw with.</summary>
    public Texture2D Texture;

    /// <summary>Resolved placement and appearance.</summary>
    public SpriteDraw Draw;

    /// <summary>Order the sprite was found in; the final tie-breaker so sorting is deterministic.</summary>
    public int Order;
}
