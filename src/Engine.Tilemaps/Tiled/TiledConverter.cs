using System.Numerics;

namespace Engine.Tilemaps.Tiled;

/// <summary>
/// Turns a parsed <see cref="TiledMap"/> into engine data: tile layers (Y flipped to point up, flip flags moved into <see cref="TileId"/>,
/// split by tile set), tile set descriptions (solid tiles from a boolean <c>solid</c> property, animations), and objects in world space.
/// Pure: it loads no textures and creates no entities (see <see cref="TiledSpawner"/> for that).
/// </summary>
public static class TiledConverter
{
    /// <summary>Converts a map.</summary>
    /// <param name="map">The parsed map.</param>
    /// <param name="options">Scale and texture naming; defaults are used when null.</param>
    /// <exception cref="InvalidOperationException">The tiles are not square, a layer uses a tile value no tile set owns, or a tile id is outside its set.</exception>
    public static ImportedMap Convert(TiledMap map, TiledImportOptions? options = null)
    {
        options ??= new TiledImportOptions();
        if (options.WorldUnitsPerPixel <= 0f) throw new ArgumentOutOfRangeException(nameof(options), "WorldUnitsPerPixel must be positive.");
        if (map.TileWidth != map.TileHeight)
            throw new InvalidOperationException($"Tiled map has {map.TileWidth}x{map.TileHeight} tiles; only square tiles are supported.");

        float scale = options.WorldUnitsPerPixel;
        float tileSize = map.TileWidth * scale;

        var tileSets = new List<ImportedTileSet>();
        var usedNames = new HashSet<string>();
        for (int i = 0; i < map.TileSets.Count; i++) tileSets.Add(ConvertTileSet(map.TileSets[i], i, options, usedNames));

        var layers = new List<ImportedLayer>();
        for (int order = 0; order < map.TileLayers.Count; order++)
            layers.AddRange(ConvertLayer(map, map.TileLayers[order], order, tileSets, tileSize, scale));

        float mapHeightPixels = map.Height * map.TileHeight;
        var objects = new List<ImportedObject>();
        foreach (var layer in map.ObjectLayers)
        {
            foreach (var o in layer.Objects)
            {
                float x = o.X + layer.OffsetX;
                float y = o.Y + layer.OffsetY;
                objects.Add(new ImportedObject(
                    layer.Name,
                    o.Name,
                    o.Class,
                    new Vector2(x * scale, (mapHeightPixels - y) * scale),
                    new Vector2(o.Width * scale, o.Height * scale),
                    o.Gid,
                    o.IsPoint,
                    o.Properties));
            }
        }

        return new ImportedMap(tileSets, layers, objects, new Vector2(map.Width * tileSize, map.Height * tileSize), tileSize);
    }

    private static ImportedTileSet ConvertTileSet(TiledTileSet source, int index, TiledImportOptions options, HashSet<string> usedNames)
    {
        string name = string.IsNullOrWhiteSpace(source.Name) ? $"tileset{index}" : source.Name;
        while (!usedNames.Add(name)) name += "_";

        var data = new TileSetData
        {
            Texture = options.TextureFolder + Path.GetFileName(source.Image.Replace('\\', '/')),
            TileWidth = source.TileWidth,
            TileHeight = source.TileHeight,
            Spacing = source.Spacing,
            Margin = source.Margin,
            Columns = source.Columns,
            Rows = (source.TileCount + source.Columns - 1) / source.Columns,
        };

        foreach (var tile in source.Tiles)
        {
            // Tiled numbers tiles from 0 inside a set; the engine from 1 (0 means empty).
            if (tile.Properties.GetBool("solid")) data.Solid.Add(tile.Id + 1);
            if (tile.Animation.Count == 0) continue;

            var animation = new TileAnimationData { Tile = tile.Id + 1 };
            foreach (var frame in tile.Animation)
                animation.Frames.Add(new TileAnimationFrameData { Tile = frame.LocalId + 1, Duration = frame.DurationMs / 1000f });
            data.Animations.Add(animation);
        }

        return new ImportedTileSet(name, data.Texture, data);
    }

    private static IEnumerable<ImportedLayer> ConvertLayer(
        TiledMap map, TiledTileLayer source, int order, List<ImportedTileSet> tileSets, float tileSize, float scale)
    {
        // One engine layer per tile set the Tiled layer touches (an engine layer draws with a single atlas).
        var byTileSet = new Dictionary<int, TileLayer>();
        foreach (var cell in source.Cells)
        {
            uint gid = TileId.IdFromTiled(cell.Gid);
            int set = TileSetIndexOf(map.TileSets, gid);
            if (set < 0)
                throw new InvalidOperationException($"Tiled layer '{source.Name}': tile value {gid} at ({cell.X}, {cell.Y}) belongs to no tile set.");

            var tileSet = map.TileSets[set];
            int local = (int)(gid - (uint)tileSet.FirstGid);
            if (local >= tileSet.TileCount)
                throw new InvalidOperationException($"Tiled layer '{source.Name}': tile {local} is outside tile set '{tileSet.Name}' ({tileSet.TileCount} tiles).");

            if (!byTileSet.TryGetValue(set, out var layer))
            {
                layer = new TileLayer(tileSize);
                byTileSet[set] = layer;
            }

            // Tiled rows count downwards from the top; engine cells count upwards from the bottom.
            int y = map.Height - 1 - cell.Y;
            layer.SetTile(cell.X, y, TileId.With(local + 1, TileId.FlagsFromTiled(cell.Gid)));
        }

        bool foreground = source.Properties.GetBool("foreground");
        var position = new Vector2(source.OffsetX * scale, -source.OffsetY * scale);
        var parallax = new Vector2(1f - source.ParallaxX, 1f - source.ParallaxY);
        foreach (var (set, layer) in byTileSet.OrderBy(pair => pair.Key))
        {
            string name = byTileSet.Count == 1 ? source.Name : $"{source.Name}#{tileSets[set].Name}";
            yield return new ImportedLayer(name, layer, tileSets[set].Name, position, parallax, foreground, source.Visible, source.Opacity, order);
        }
    }

    /// <summary>The index of the tile set that owns a global id: the one with the largest first id not above it; -1 when none does.</summary>
    private static int TileSetIndexOf(IReadOnlyList<TiledTileSet> tileSets, uint gid)
    {
        for (int i = tileSets.Count - 1; i >= 0; i--)
        {
            if (gid >= (uint)tileSets[i].FirstGid) return i;
        }

        return -1;
    }
}
