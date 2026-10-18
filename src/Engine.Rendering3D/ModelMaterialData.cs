using Engine.Rendering;

namespace Engine.Rendering3D;

/// <summary>A material as read from a model file, before any GPU object exists: factors, and the still-encoded images.</summary>
public sealed record ModelMaterialData
{
    /// <summary>What a primitive without a material gets (glTF's default: white, fully rough, metallic factor 1 per the spec).</summary>
    public static ModelMaterialData Default { get; } = new() { Metallic = 1f, Roughness = 1f };

    /// <summary>Colour multiplier (glTF baseColorFactor).</summary>
    public Color BaseColor { get; init; } = Color.White;

    /// <summary>Encoded PNG or JPEG bytes of the base colour texture, or null for a plain colour.</summary>
    public byte[]? Image { get; init; }

    /// <summary>0 = dielectric, 1 = metal (glTF metallicFactor).</summary>
    public float Metallic { get; init; }

    /// <summary>0 = mirror, 1 = rough (glTF roughnessFactor).</summary>
    public float Roughness { get; init; } = 1f;

    /// <summary>Encoded bytes of the roughness (green) and metallic (blue) map, or null.</summary>
    public byte[]? MetallicRoughnessImage { get; init; }

    /// <summary>Encoded bytes of the tangent-space normal map, or null.</summary>
    public byte[]? NormalImage { get; init; }

    /// <summary>Strength of the normal map (glTF normalTexture.scale).</summary>
    public float NormalScale { get; init; } = 1f;

    /// <summary>Emitted colour (glTF emissiveFactor), black for none.</summary>
    public Color Emissive { get; init; } = new(0f, 0f, 0f);

    /// <summary>How alpha is used.</summary>
    public AlphaMode AlphaMode { get; init; } = AlphaMode.Opaque;

    /// <summary>Cut-off alpha for <see cref="AlphaMode.Mask"/>.</summary>
    public float AlphaCutoff { get; init; } = 0.5f;

    /// <summary>Draw back faces too.</summary>
    public bool DoubleSided { get; init; }
}
