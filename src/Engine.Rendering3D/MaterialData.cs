namespace Engine.Rendering3D;

/// <summary>The parsed contents of a <c>.material.json</c> file, before the texture name is resolved to an asset handle.</summary>
public sealed class MaterialData
{
    /// <summary>Base colour as 3 (RGB) or 4 (RGBA) numbers in 0..1; null means white.</summary>
    public float[]? BaseColor { get; set; }

    /// <summary>Asset name of the colour texture, or null for none.</summary>
    public string? Texture { get; set; }

    /// <summary>Specular exponent.</summary>
    public float Shininess { get; set; } = 32f;

    /// <summary>Specular strength, 0..1.</summary>
    public float Specular { get; set; } = 0.3f;

    /// <summary>Ignore lighting.</summary>
    public bool Unlit { get; set; }

    /// <summary>Draw back faces too.</summary>
    public bool DoubleSided { get; set; }
}
