namespace Engine.Rendering;

/// <summary>How a batch combines the sprites it draws with the pixels already in the target.</summary>
public enum SpriteBlend
{
    /// <summary>Normal transparency: the sprite covers what is behind it in proportion to its alpha.</summary>
    Alpha,

    /// <summary>The sprite's colour (scaled by its alpha) is added to what is behind it, so overlapping lights brighten each other.</summary>
    Additive,
}
