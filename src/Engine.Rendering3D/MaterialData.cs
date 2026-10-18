namespace Engine.Rendering3D;

/// <summary>The parsed contents of a <c>.material.json</c> file, before the texture names are resolved to asset handles.</summary>
public sealed class MaterialData
{
    /// <summary>Base colour as 3 (RGB) or 4 (RGBA) numbers in 0..1; null means white.</summary>
    public float[]? BaseColor { get; set; }

    /// <summary>Asset name of the colour texture, or null for none.</summary>
    public string? Texture { get; set; }

    /// <summary>0 (dielectric) to 1 (metal).</summary>
    public float Metallic { get; set; }

    /// <summary>0 (mirror) to 1 (rough).</summary>
    public float Roughness { get; set; } = 0.6f;

    /// <summary>Asset name of the roughness (green) and metallic (blue) map, or null for none.</summary>
    public string? MetallicRoughnessTexture { get; set; }

    /// <summary>Asset name of the tangent-space normal map, or null for none.</summary>
    public string? NormalTexture { get; set; }

    /// <summary>Strength of the normal map.</summary>
    public float NormalScale { get; set; } = 1f;

    /// <summary>Emitted colour as 3 numbers in 0..1; null means none.</summary>
    public float[]? Emissive { get; set; }

    /// <summary><c>opaque</c> (default), <c>mask</c> or <c>blend</c>.</summary>
    public string? AlphaMode { get; set; }

    /// <summary>Cut-off alpha for <c>mask</c>.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>Ignore lighting.</summary>
    public bool Unlit { get; set; }

    /// <summary>Draw back faces too.</summary>
    public bool DoubleSided { get; set; }

    /// <summary>Cast shadows (default true).</summary>
    public bool CastShadows { get; set; } = true;
}
