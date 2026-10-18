using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// A light at the entity's position that shines in every direction and fades smoothly to nothing at <see cref="Radius"/>. The
/// 3D counterpart of <c>Light2D</c>. Plain data; <see cref="MeshRenderSystem"/> reads it.
/// </summary>
public struct PointLight3D
{
    /// <summary>Light colour.</summary>
    public Color Color;

    /// <summary>Strength multiplier on <see cref="Color"/>; 0 switches the light off.</summary>
    public float Intensity;

    /// <summary>Distance in world units at which the light has faded to zero; also what limits which meshes it touches.</summary>
    public float Radius;

    /// <summary>A light of the given radius and colour at intensity 1.</summary>
    public static PointLight3D Create(float radius, Color color, float intensity = 1f) =>
        new() { Color = color, Intensity = intensity, Radius = radius };
}
