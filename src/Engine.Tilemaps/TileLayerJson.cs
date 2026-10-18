using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Engine.Tilemaps;

/// <summary>Reads and writes tile layers as JSON (<c>.layer.json</c> files): a dense grid of ids that is easy to read and edit by hand.</summary>
/// <remarks>
/// Example: <c>{ "tileSize": 32, "originX": 0, "originY": 0, "rows": [ [3,3,3], [3,0,3], [3,3,3] ] }</c>
/// </remarks>
public static class TileLayerJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Parses a layer from JSON text.</summary>
    /// <exception cref="InvalidOperationException">The JSON is invalid, the tile size is not positive, or an id is negative.</exception>
    public static TileLayer Read(string json)
    {
        TileLayerData data;
        try
        {
            data = JsonSerializer.Deserialize<TileLayerData>(json, Options)
                ?? throw new InvalidOperationException("The tile layer file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Tile layer file is not valid: {ex.Message}", ex);
        }

        if (data.TileSize <= 0f) throw new InvalidOperationException("Tile layer: 'tileSize' must be positive.");

        var layer = new TileLayer(data.TileSize);
        for (int row = 0; row < data.Rows.Count; row++)
        {
            int y = data.OriginY + (data.Rows.Count - 1 - row); // the first listed row is the top one
            var ids = data.Rows[row];
            for (int column = 0; column < ids.Length; column++)
            {
                if (ids[column] < 0) throw new InvalidOperationException($"Tile layer: negative tile id {ids[column]} in row {row}.");
                if (ids[column] != 0) layer.SetTile(data.OriginX + column, y, ids[column]);
            }
        }

        return layer;
    }

    /// <summary>
    /// Writes a layer covering its used area, one text line per row so the file reads like a map. An empty layer has no rows.
    /// </summary>
    public static string Write(TileLayer layer)
    {
        var text = new StringBuilder();
        text.AppendLine("{");
        text.AppendLine($"  \"tileSize\": {layer.TileSize.ToString(CultureInfo.InvariantCulture)},");

        if (!layer.TryGetBounds(out int minX, out int minY, out int maxX, out int maxY))
        {
            text.AppendLine("  \"originX\": 0,");
            text.AppendLine("  \"originY\": 0,");
            text.AppendLine("  \"rows\": []");
            text.Append('}');
            return text.ToString();
        }

        text.AppendLine($"  \"originX\": {minX},");
        text.AppendLine($"  \"originY\": {minY},");
        text.AppendLine("  \"rows\": [");
        for (int y = maxY; y >= minY; y--)
        {
            var ids = Enumerable.Range(minX, maxX - minX + 1).Select(x => layer.GetTile(x, y));
            text.Append("    [").Append(string.Join(", ", ids)).Append(']');
            text.AppendLine(y > minY ? "," : "");
        }

        text.AppendLine("  ]");
        text.Append('}');
        return text.ToString();
    }
}
