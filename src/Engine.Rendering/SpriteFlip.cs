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
}
