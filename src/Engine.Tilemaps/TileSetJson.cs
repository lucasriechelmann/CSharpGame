using System.Text.Json;

namespace Engine.Tilemaps;

/// <summary>Reads tile set descriptions from JSON (<c>.tileset.json</c> files).</summary>
/// <remarks>
/// Example: <c>{ "texture": "tiles.png", "tileWidth": 16, "tileHeight": 16, "solid": [1, 2, 3] }</c>
/// </remarks>
public static class TileSetJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Parses the description. Turn it into a <see cref="TileSet"/> with <see cref="TileSetData.CreateTileSet"/> once the texture is loaded.</summary>
    /// <exception cref="InvalidOperationException">The JSON is invalid or names no texture.</exception>
    public static TileSetData Read(string json)
    {
        TileSetData data;
        try
        {
            data = JsonSerializer.Deserialize<TileSetData>(json, Options)
                ?? throw new InvalidOperationException("The tile set file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Tile set file is not valid: {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(data.Texture)) throw new InvalidOperationException("Tile set: 'texture' is required.");
        return data;
    }
}
