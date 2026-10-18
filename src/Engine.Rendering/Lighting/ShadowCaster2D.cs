using System.Numerics;

namespace Engine.Rendering;

/// <summary>
/// Makes an entity block light: a box centred on the entity (turned and scaled by its transform) whose back walls throw shadows away
/// from every <see cref="Light2D"/> when the <see cref="LightingEffect"/> has shadows on. Plain data, saved in scenes.
/// </summary>
public struct ShadowCaster2D
{
    /// <summary>Width and height of the box in world units, before the entity's scale.</summary>
    public Vector2 Size;

    /// <summary>A box caster of the given size.</summary>
    /// <param name="size">Width and height in world units.</param>
    public static ShadowCaster2D Box(Vector2 size) => new() { Size = size };
}
