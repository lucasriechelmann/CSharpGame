namespace Engine.Rendering;

/// <summary>Mirroring applied to a sprite's texture coordinates.</summary>
[Flags]
public enum SpriteFlip
{
    /// <summary>Draw as stored.</summary>
    None = 0,

    /// <summary>Mirror left-to-right (e.g. a character facing the other way).</summary>
    Horizontal = 1,

    /// <summary>Mirror top-to-bottom.</summary>
    Vertical = 2,

    /// <summary>Swap the texture's x and y axes (mirror along the diagonal). Applied before <see cref="Horizontal"/> and <see cref="Vertical"/>, so the three together give all eight orientations of a square tile.</summary>
    Diagonal = 4,
}
