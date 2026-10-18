using System.Text.Json;

namespace Engine.Rendering3D;

/// <summary>Reads material descriptions from JSON (<c>.material.json</c> files).</summary>
/// <remarks>
/// Example: <c>{ "baseColor": [0.8, 0.2, 0.2], "texture": "bricks.png", "shininess": 64, "specular": 0.5 }</c>. Every key is optional.
/// </remarks>
public static class MaterialJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Parses and validates the description.</summary>
    /// <exception cref="InvalidOperationException">The JSON is invalid, the colour does not have 3 or 4 numbers, or shininess is not positive.</exception>
    public static MaterialData Read(string json)
    {
        MaterialData data;
        try
        {
            data = JsonSerializer.Deserialize<MaterialData>(json, Options)
                ?? throw new InvalidOperationException("The material file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Material file is not valid: {ex.Message}", ex);
        }

        if (data.BaseColor != null && data.BaseColor.Length is not (3 or 4))
            throw new InvalidOperationException($"Material: 'baseColor' needs 3 (RGB) or 4 (RGBA) numbers, got {data.BaseColor.Length}.");
        if (data.Shininess <= 0f)
            throw new InvalidOperationException($"Material: 'shininess' must be positive, got {data.Shininess}.");
        return data;
    }
}
