using System.Text.Json;

namespace Engine.Tilemaps.Tiled;

/// <summary>
/// Reads Tiled maps saved as JSON (<c>.tmj</c>, with <c>.tsj</c> tile sets embedded or external) into plain data. Supports orthogonal maps,
/// finite and infinite (chunked), tile layers with array, base64, gzip or zlib data, object layers (rectangles, points, tile objects), group
/// layers (flattened), tile animations and custom properties. Everything else that would change how a map looks throws, naming the problem.
/// </summary>
/// <remarks>Split by topic: this file (the map and tile sets), <c>TiledReader.Layers.cs</c> (layers and objects).</remarks>
public static partial class TiledReader
{
    /// <summary>Reads a map file; relative tile set files are read from the same folder.</summary>
    /// <param name="path">Path of the <c>.tmj</c> file.</param>
    /// <exception cref="FileNotFoundException">The map or one of its external tile sets is missing.</exception>
    /// <exception cref="InvalidOperationException">The file is not a map this reader supports.</exception>
    public static TiledMap ReadFile(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Tiled map '{path}' not found.", fullPath);

        string folder = Path.GetDirectoryName(fullPath)!;
        return Read(File.ReadAllText(fullPath), source =>
        {
            string tileSetPath = Path.Combine(folder, source);
            if (!File.Exists(tileSetPath)) throw new FileNotFoundException($"Tiled tile set '{source}' (named by '{path}') not found.", tileSetPath);
            return File.ReadAllText(tileSetPath);
        });
    }

    /// <summary>Reads a map from JSON text.</summary>
    /// <param name="json">The <c>.tmj</c> text.</param>
    /// <param name="readExternal">Returns the text of an external tile set file given its <c>source</c> path; null means external tile sets are an error.</param>
    /// <exception cref="InvalidOperationException">The text is not a supported map.</exception>
    public static TiledMap Read(string json, Func<string, string>? readExternal = null)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Tiled map is not valid JSON: {ex.Message}", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("A Tiled map must be a JSON object.");

            string orientation = String(root, "orientation", "orthogonal");
            if (orientation != "orthogonal")
                throw new InvalidOperationException($"Tiled map orientation '{orientation}' is not supported; only orthogonal maps are.");

            bool infinite = Bool(root, "infinite", false);
            int tileWidth = Int(root, "tilewidth", 0);
            int tileHeight = Int(root, "tileheight", 0);
            if (tileWidth <= 0 || tileHeight <= 0) throw new InvalidOperationException("Tiled map: 'tilewidth' and 'tileheight' must be positive.");
            int width = infinite ? 0 : Int(root, "width", 0);
            int height = infinite ? 0 : Int(root, "height", 0);
            if (!infinite && (width <= 0 || height <= 0)) throw new InvalidOperationException("Tiled map: 'width' and 'height' must be positive.");

            var tileSets = new List<TiledTileSet>();
            if (root.TryGetProperty("tilesets", out var tileSetArray))
            {
                foreach (var entry in tileSetArray.EnumerateArray()) tileSets.Add(ReadTileSet(entry, readExternal));
            }

            tileSets.Sort((a, b) => a.FirstGid.CompareTo(b.FirstGid));

            var tileLayers = new List<TiledTileLayer>();
            var objectLayers = new List<TiledObjectLayer>();
            if (root.TryGetProperty("layers", out var layerArray)) ReadLayers(layerArray, 0f, 0f, tileLayers, objectLayers);

            return new TiledMap(width, height, tileWidth, tileHeight, tileSets, tileLayers, objectLayers, ReadProperties(root));
        }
    }

    private static TiledTileSet ReadTileSet(JsonElement entry, Func<string, string>? readExternal)
    {
        int firstGid = Int(entry, "firstgid", 1);
        var element = entry;
        JsonDocument? external = null;
        try
        {
            if (entry.TryGetProperty("source", out var source))
            {
                string file = source.GetString() ?? "";
                if (readExternal == null)
                    throw new InvalidOperationException($"Tiled map uses the external tile set '{file}', but no way to read it was given.");
                try
                {
                    external = JsonDocument.Parse(readExternal(file));
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Tiled tile set '{file}' is not valid JSON (only .tsj tile sets are supported): {ex.Message}", ex);
                }

                element = external.RootElement;
            }

            string name = String(element, "name", "");
            string image = String(element, "image", "");
            if (image.Length == 0)
                throw new InvalidOperationException($"Tiled tile set '{name}' has no single image; tile sets that are collections of images are not supported.");

            int tileWidth = Int(element, "tilewidth", 0);
            int tileHeight = Int(element, "tileheight", 0);
            if (tileWidth <= 0 || tileHeight <= 0) throw new InvalidOperationException($"Tiled tile set '{name}': 'tilewidth' and 'tileheight' must be positive.");

            int spacing = Int(element, "spacing", 0);
            int margin = Int(element, "margin", 0);
            int imageWidth = Int(element, "imagewidth", 0);
            int imageHeight = Int(element, "imageheight", 0);
            int columns = Int(element, "columns", 0);
            if (columns <= 0 && imageWidth > 0) columns = (imageWidth - 2 * margin + spacing) / (tileWidth + spacing);
            int tileCount = Int(element, "tilecount", 0);
            if (tileCount <= 0 && columns > 0 && imageHeight > 0) tileCount = columns * ((imageHeight - 2 * margin + spacing) / (tileHeight + spacing));
            if (columns <= 0 || tileCount <= 0) throw new InvalidOperationException($"Tiled tile set '{name}': its size could not be worked out (missing columns, tilecount or image size).");

            var tiles = new List<TiledTile>();
            if (element.TryGetProperty("tiles", out var tileArray))
            {
                foreach (var tile in tileArray.EnumerateArray())
                {
                    var frames = new List<TiledAnimationFrame>();
                    if (tile.TryGetProperty("animation", out var animation))
                    {
                        foreach (var frame in animation.EnumerateArray())
                            frames.Add(new TiledAnimationFrame(Int(frame, "tileid", 0), Int(frame, "duration", 100)));
                    }

                    string tileClass = String(tile, "class", String(tile, "type", ""));
                    tiles.Add(new TiledTile(Int(tile, "id", 0), tileClass, ReadProperties(tile), frames));
                }
            }

            return new TiledTileSet(name, firstGid, tileWidth, tileHeight, spacing, margin, columns, tileCount, image, imageWidth, imageHeight, tiles);
        }
        finally
        {
            external?.Dispose();
        }
    }

    /// <summary>The custom properties of an element as name to text (Tiled writes them as an array of name, type and value).</summary>
    private static TiledProperties ReadProperties(JsonElement element)
    {
        if (!element.TryGetProperty("properties", out var array) || array.ValueKind != JsonValueKind.Array) return TiledProperties.Empty;

        var values = new Dictionary<string, string>();
        foreach (var property in array.EnumerateArray())
        {
            string name = String(property, "name", "");
            if (name.Length == 0 || !property.TryGetProperty("value", out var value)) continue;

            values[name] = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText();
        }

        return new TiledProperties(values);
    }

    private static string String(JsonElement element, string name, string fallback) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;

    private static int Int(JsonElement element, string name, int fallback) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? (int)Math.Round(value.GetDouble()) : fallback;

    private static float Float(JsonElement element, string name, float fallback) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? (float)value.GetDouble() : fallback;

    private static bool Bool(JsonElement element, string name, bool fallback) =>
        element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
}
