using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// Scene-wide lighting settings that are not an entity: the constant ambient term, which stands in for light bounced from everywhere so
/// unlit sides are not pitch black, and a two-colour sky that shiny surfaces reflect (there is no environment map, and a metal with
/// nothing to reflect would be black), and the sun's shadow settings. The lights themselves are <see cref="DirectionalLight"/> and <see cref="PointLight3D"/> components.
/// </summary>
public sealed class Lighting3D
{
    /// <summary>Colour added to every lit surface regardless of orientation (multiplied by the surface colour).</summary>
    public Color Ambient { get; set; } = new(0.2f, 0.22f, 0.28f);

    /// <summary>What a surface reflects when it looks up: the colour of the sky overhead (sRGB). Mirrors the clear colour a game draws its sky with.</summary>
    public Color ReflectedSky { get; set; } = new(0.45f, 0.62f, 0.85f);

    /// <summary>What a surface reflects when it looks down: the colour of the ground (sRGB). Between the two the reflection blends smoothly.</summary>
    public Color ReflectedGround { get; set; } = new(0.25f, 0.24f, 0.22f);

    /// <summary>How much of the sky and ground colour shiny surfaces pick up, 0 (none: metals go black without lights) to 1.</summary>
    public float ReflectionStrength { get; set; } = 0.8f;

    /// <summary>Whether the sun casts shadows (one shadow map fitted to the part of the view nearest the camera). Off saves the extra depth pass.</summary>
    public bool ShadowsEnabled { get; set; } = true;

    /// <summary>How far from the camera, in world units, shadows are drawn. Shorter is sharper (the map's texels cover less ground); nothing beyond it is shadowed.</summary>
    public float ShadowDistance { get; set; } = 50f;

    /// <summary>
    /// Depth bias against self-shadowing, in shadow-map depth units (0 to 1 over the map's depth range). Too small speckles lit surfaces with
    /// shadow; too large lifts shadows off their casters ("peter panning"). It grows automatically on surfaces that face away from the sun.
    /// </summary>
    public float ShadowBias { get; set; } = 0.0008f;
}
