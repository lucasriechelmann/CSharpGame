using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>
/// A light infinitely far away, like the sun: every surface receives parallel rays. It shines along the entity's forward axis
/// (-Z of its <c>Transform.Rotation</c>), so rotate the entity to aim it; its position does not matter. Only the first one found is
/// used. Plain data; <see cref="MeshRenderSystem"/> reads it.
/// </summary>
public struct DirectionalLight
{
    /// <summary>Light colour.</summary>
    public Color Color;

    /// <summary>Strength multiplier on <see cref="Color"/>; 0 switches the light off.</summary>
    public float Intensity;

    /// <summary>A white-ish light of the given strength.</summary>
    public static DirectionalLight Create(float intensity = 1f) => new() { Color = new Color(1f, 0.96f, 0.88f), Intensity = intensity };
}
