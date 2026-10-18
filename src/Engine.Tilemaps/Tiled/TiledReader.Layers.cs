using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;

namespace Engine.Tilemaps.Tiled;

// Layers and objects: tile data in its several encodings, chunks of infinite maps, object layers, and group layers (flattened).
public static partial class TiledReader
{
    private static void ReadLayers(JsonElement layers, float groupOffsetX, float groupOffsetY, List<TiledTileLayer> tileLayers, List<TiledObjectLayer> objectLayers)
    {
        foreach (var layer in layers.EnumerateArray())
        {
            string type = String(layer, "type", "");
            string name = String(layer, "name", "");
            float offsetX = groupOffsetX + Float(layer, "offsetx", 0f);
            float offsetY = groupOffsetY + Float(layer, "offsety", 0f);

            switch (type)
            {
                case "tilelayer":
                    tileLayers.Add(new TiledTileLayer(
                        name,
                        Bool(layer, "visible", true),
                        Float(layer, "opacity", 1f),
                        offsetX,
                        offsetY,
                        Float(layer, "parallaxx", 1f),
                        Float(layer, "parallaxy", 1f),
                        ReadProperties(layer),
                        ReadCells(layer, name)));
                    break;

                case "objectgroup":
                    objectLayers.Add(new TiledObjectLayer(name, offsetX, offsetY, ReadObjects(layer)));
                    break;

                case "group":
                    // A group only organises layers (and offsets them together), so its children are flattened into the map's lists.
                    if (layer.TryGetProperty("layers", out var children)) ReadLayers(children, offsetX, offsetY, tileLayers, objectLayers);
                    break;

                default:
                    break; // image layers carry no tiles; nothing to import
            }
        }
    }

    private static List<TiledCell> ReadCells(JsonElement layer, string layerName)
    {
        var cells = new List<TiledCell>();
        string encoding = String(layer, "encoding", "csv");
        string compression = String(layer, "compression", "");

        if (layer.TryGetProperty("chunks", out var chunks))
        {
            foreach (var chunk in chunks.EnumerateArray())
            {
                AddCells(cells, chunk, Int(chunk, "x", 0), Int(chunk, "y", 0), Int(chunk, "width", 0), encoding, compression, layerName);
            }

            return cells;
        }

        AddCells(cells, layer, Int(layer, "startx", 0), Int(layer, "starty", 0), Int(layer, "width", 0), encoding, compression, layerName);
        return cells;
    }

    /// <summary>Decodes one block of tile data (a whole finite layer or one chunk) and adds its non-empty cells; row-major, <paramref name="width"/> cells per row, from the given corner.</summary>
    private static void AddCells(List<TiledCell> cells, JsonElement block, int originX, int originY, int width, string encoding, string compression, string layerName)
    {
        if (!block.TryGetProperty("data", out var data)) return;
        if (width <= 0) throw new InvalidOperationException($"Tiled layer '{layerName}': tile data without a width.");

        uint[] gids;
        if (data.ValueKind == JsonValueKind.Array)
        {
            gids = new uint[data.GetArrayLength()];
            int i = 0;
            foreach (var value in data.EnumerateArray()) gids[i++] = value.GetUInt32();
        }
        else if (data.ValueKind == JsonValueKind.String)
        {
            if (encoding != "base64")
                throw new InvalidOperationException($"Tiled layer '{layerName}': data encoding '{encoding}' is not supported (use CSV or base64).");
            gids = DecodeBase64(data.GetString() ?? "", compression, layerName);
        }
        else
        {
            throw new InvalidOperationException($"Tiled layer '{layerName}': tile data must be an array or a base64 string.");
        }

        for (int i = 0; i < gids.Length; i++)
        {
            if (gids[i] != 0) cells.Add(new TiledCell(originX + i % width, originY + i / width, gids[i]));
        }
    }

    /// <summary>Base64 text to little-endian 32-bit global ids, undoing gzip or zlib compression when the layer says so.</summary>
    private static uint[] DecodeBase64(string text, string compression, string layerName)
    {
        byte[] bytes = Convert.FromBase64String(text);
        switch (compression)
        {
            case "":
                break;
            case "gzip":
                bytes = Inflate(new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress));
                break;
            case "zlib":
                bytes = Inflate(new ZLibStream(new MemoryStream(bytes), CompressionMode.Decompress));
                break;
            default:
                throw new InvalidOperationException($"Tiled layer '{layerName}': compression '{compression}' is not supported (use none, gzip or zlib).");
        }

        if (bytes.Length % 4 != 0) throw new InvalidOperationException($"Tiled layer '{layerName}': tile data is {bytes.Length} bytes, which is not a whole number of 32-bit ids.");

        var gids = new uint[bytes.Length / 4];
        for (int i = 0; i < gids.Length; i++) gids[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i * 4, 4));
        return gids;
    }

    private static byte[] Inflate(Stream stream)
    {
        using (stream)
        {
            var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }
    }

    private static List<TiledObject> ReadObjects(JsonElement layer)
    {
        var objects = new List<TiledObject>();
        if (!layer.TryGetProperty("objects", out var array)) return objects;

        foreach (var entry in array.EnumerateArray())
        {
            uint gid = entry.TryGetProperty("gid", out var g) && g.ValueKind == JsonValueKind.Number ? g.GetUInt32() : 0u;
            objects.Add(new TiledObject(
                Int(entry, "id", 0),
                String(entry, "name", ""),
                String(entry, "class", String(entry, "type", "")),
                Float(entry, "x", 0f),
                Float(entry, "y", 0f),
                Float(entry, "width", 0f),
                Float(entry, "height", 0f),
                gid,
                Bool(entry, "point", false),
                ReadProperties(entry)));
        }

        return objects;
    }
}
