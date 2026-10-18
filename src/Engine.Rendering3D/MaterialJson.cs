using System.Text.Json;
using System.Text.Json.Nodes;

namespace Engine.Rendering3D;

/// <summary>Reads material descriptions from JSON (<c>.material.json</c> files).</summary>
/// <remarks>
/// Example: <c>{ "baseColor": [0.8, 0.2, 0.2], "texture": "bricks.png", "metallic": 0, "roughness": 0.4, "normalTexture": "bricks_n.png" }</c>.
/// Every key is optional; an unknown key is an error (a typo would otherwise silently do nothing).
/// </remarks>
public static class MaterialJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "baseColor", "texture", "metallic", "roughness", "metallicRoughnessTexture", "normalTexture", "normalScale",
        "emissive", "alphaMode", "alphaCutoff", "unlit", "doubleSided", "castShadows",
    };

    /// <summary>Parses and validates the description.</summary>
    /// <exception cref="InvalidOperationException">The JSON is invalid, a key is unknown (the old <c>shininess</c> and <c>specular</c> say what to use now), a colour has the wrong number of numbers, or a value is out of range.</exception>
    public static MaterialData Read(string json)
    {
        MaterialData data;
        try
        {
            if (JsonNode.Parse(json) is not JsonObject root) throw new InvalidOperationException("The material file must be a JSON object.");
            foreach (var (key, _) in root)
            {
                if (key.Equals("shininess", StringComparison.OrdinalIgnoreCase) || key.Equals("specular", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Material: '{key}' is gone; materials are metallic-roughness now, use 'roughness' (0 mirror .. 1 rough) and 'metallic' (0..1).");
                if (!KnownKeys.Contains(key)) throw new InvalidOperationException($"Material: unknown key '{key}'.");
            }

            data = root.Deserialize<MaterialData>(Options) ?? throw new InvalidOperationException("The material file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Material file is not valid: {ex.Message}", ex);
        }

        if (data.BaseColor != null && data.BaseColor.Length is not (3 or 4))
            throw new InvalidOperationException($"Material: 'baseColor' needs 3 (RGB) or 4 (RGBA) numbers, got {data.BaseColor.Length}.");
        if (data.Emissive != null && data.Emissive.Length != 3)
            throw new InvalidOperationException($"Material: 'emissive' needs 3 (RGB) numbers, got {data.Emissive.Length}.");
        if (data.Metallic is < 0f or > 1f)
            throw new InvalidOperationException($"Material: 'metallic' must be between 0 and 1, got {data.Metallic}.");
        if (data.Roughness is < 0f or > 1f)
            throw new InvalidOperationException($"Material: 'roughness' must be between 0 and 1, got {data.Roughness}.");
        if (data.AlphaCutoff is < 0f or > 1f)
            throw new InvalidOperationException($"Material: 'alphaCutoff' must be between 0 and 1, got {data.AlphaCutoff}.");
        if (data.AlphaMode != null) ParseAlphaMode(data.AlphaMode);
        return data;
    }

    /// <summary>The <see cref="AlphaMode"/> named in a file (case-insensitive); null means opaque.</summary>
    /// <param name="name">The text from the file.</param>
    /// <exception cref="InvalidOperationException">The name is not opaque, mask or blend.</exception>
    public static AlphaMode ParseAlphaMode(string? name) => name?.ToLowerInvariant() switch
    {
        null or "opaque" => AlphaMode.Opaque,
        "mask" => AlphaMode.Mask,
        "blend" => AlphaMode.Blend,
        _ => throw new InvalidOperationException($"Material: 'alphaMode' must be opaque, mask or blend, got '{name}'."),
    };
}
