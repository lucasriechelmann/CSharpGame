using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// Scene-wide lighting settings that are not an entity: the constant ambient term, which stands in for light bounced from
/// everywhere so unlit sides are not pitch black. The lights themselves are <see cref="DirectionalLight"/> and
/// <see cref="PointLight3D"/> components.
/// </summary>
public sealed class Lighting3D
{
    /// <summary>Colour added to every lit surface regardless of orientation (multiplied by the surface colour).</summary>
    public Color Ambient { get; set; } = new(0.2f, 0.22f, 0.28f);
}
