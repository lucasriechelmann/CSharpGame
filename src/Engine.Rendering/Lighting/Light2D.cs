namespace Engine.Rendering;

/// <summary>
/// A point light: an entity with a <c>Transform</c> and a <see cref="Light2D"/> brightens the area around it when a
/// <see cref="LightingEffect"/> is in the post-processing chain. Plain data, so lights can be placed in scene files and edited
/// live in the inspector.
/// </summary>
public struct Light2D
{
    /// <summary>Light colour; each channel 0..1.</summary>
    public Color Color;

    /// <summary>Brightness multiplier on <see cref="Color"/>; above 1 over-brightens the centre.</summary>
    public float Intensity;

    /// <summary>Distance in world units at which the light has faded to nothing.</summary>
    public float Radius;

    /// <summary>Creates a light. <c>default</c> is a black, zero-radius light that does nothing, so prefer this.</summary>
    /// <param name="radius">Reach in world units.</param>
    /// <param name="color">Light colour.</param>
    /// <param name="intensity">Brightness multiplier.</param>
    public static Light2D Create(float radius, Color color, float intensity = 1f) => new()
    {
        Radius = radius,
        Color = color,
        Intensity = intensity,
    };
}
