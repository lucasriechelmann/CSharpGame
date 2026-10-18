using System.IO.Compression;
using System.Numerics;
using Engine.Tilemaps;
using Engine.Tilemaps.Tiled;

namespace Engine.Tilemaps.Tests;

public class TiledImportTests
{
    private const uint FlipH = 0x80000000u;
    private const uint FlipV = 0x40000000u;
    private const uint FlipD = 0x20000000u;

    private const string TileSetJson = """
        { "firstgid": 1, "name": "tiles", "tilewidth": 16, "tileheight": 16, "image": "art/tiles.png", "imagewidth": 80, "imageheight": 32,
          "columns": 5, "tilecount": 10, "spacing": 0, "margin": 0,
          "tiles": [
            { "id": 0, "properties": [ { "name": "solid", "type": "bool", "value": true } ] },
            { "id": 4, "animation": [ { "tileid": 4, "duration": 200 }, { "tileid": 5, "duration": 300 } ] }
          ] }
        """;

    private static string Map(string layers, string tileSets = "", string extra = "", int width = 4, int height = 3) =>
        $$"""
        {
          "type": "map", "orientation": "orthogonal", "width": {{width}}, "height": {{height}}, "tilewidth": 16, "tileheight": 16, "infinite": false,
          "tilesets": [ {{(tileSets.Length > 0 ? tileSets : "{ \"firstgid\": 1, \"name\": \"tiles\", \"tilewidth\": 16, \"tileheight\": 16, \"image\": \"art/tiles.png\", \"imagewidth\": 80, \"imageheight\": 32, \"columns\": 5, \"tilecount\": 10, \"tiles\": [ { \"id\": 0, \"properties\": [ { \"name\": \"solid\", \"type\": \"bool\", \"value\": true } ] }, { \"id\": 4, \"animation\": [ { \"tileid\": 4, \"duration\": 200 }, { \"tileid\": 5, \"duration\": 300 } ] } ] }")}} ],
          "layers": [ {{layers}} ]{{extra}}
        }
        """;

    private static string GroundLayer(string data = "[0,0,0,0, 0,2,3,0, 1,1,1,1]") =>
        $$"""{ "type": "tilelayer", "name": "ground", "width": 4, "height": 3, "data": {{data}} }""";

    // ---- reading ----

    [Fact]
    public void A_small_map_reads_its_size_tile_set_and_cells()
    {
        var map = TiledReader.Read(Map(GroundLayer()));

        Assert.Equal((4, 3, 16, 16), (map.Width, map.Height, map.TileWidth, map.TileHeight));
        var tileSet = Assert.Single(map.TileSets);
        Assert.Equal("tiles", tileSet.Name);
        Assert.Equal(5, tileSet.Columns);
        Assert.Equal(10, tileSet.TileCount);
        var layer = Assert.Single(map.TileLayers);
        Assert.Equal(6, layer.Cells.Count); // six non-empty cells
        Assert.Contains(new TiledCell(1, 1, 2), layer.Cells);
        Assert.Contains(new TiledCell(3, 2, 1), layer.Cells);
    }

    [Fact]
    public void Tile_properties_and_animations_are_read()
    {
        var tileSet = TiledReader.Read(Map(GroundLayer())).TileSets[0];

        var solid = tileSet.Tiles.Single(t => t.Id == 0);
        var animated = tileSet.Tiles.Single(t => t.Id == 4);
        Assert.True(solid.Properties.GetBool("solid"));
        Assert.Equal([new TiledAnimationFrame(4, 200), new TiledAnimationFrame(5, 300)], animated.Animation);
    }

    [Fact]
    public void Layer_attributes_offsets_parallax_and_properties_are_read()
    {
        string layer = """
            { "type": "tilelayer", "name": "sky", "width": 4, "height": 3, "data": [1,0,0,0, 0,0,0,0, 0,0,0,0],
              "offsetx": 8, "offsety": -4, "parallaxx": 0.25, "parallaxy": 0.5, "opacity": 0.5, "visible": false,
              "properties": [ { "name": "foreground", "type": "bool", "value": true } ] }
            """;

        var read = TiledReader.Read(Map(layer)).TileLayers[0];

        Assert.Equal((8f, -4f), (read.OffsetX, read.OffsetY));
        Assert.Equal((0.25f, 0.5f), (read.ParallaxX, read.ParallaxY));
        Assert.Equal(0.5f, read.Opacity);
        Assert.False(read.Visible);
        Assert.True(read.Properties.GetBool("foreground"));
    }

    [Fact]
    public void Defaults_apply_when_layer_attributes_are_missing()
    {
        var read = TiledReader.Read(Map(GroundLayer())).TileLayers[0];

        Assert.True(read.Visible);
        Assert.Equal(1f, read.Opacity);
        Assert.Equal((1f, 1f), (read.ParallaxX, read.ParallaxY));
        Assert.Equal((0f, 0f), (read.OffsetX, read.OffsetY));
    }

    [Fact]
    public void Base64_data_is_decoded_as_little_endian_ids()
    {
        var bytes = new byte[12 * 4];
        BitConverter.GetBytes(2u).CopyTo(bytes, 5 * 4);
        string layer = $$"""{ "type": "tilelayer", "name": "g", "width": 4, "height": 3, "encoding": "base64", "data": "{{Convert.ToBase64String(bytes)}}" }""";

        var cells = TiledReader.Read(Map(layer)).TileLayers[0].Cells;

        Assert.Equal(new TiledCell(1, 1, 2), Assert.Single(cells));
    }

    [Theory]
    [InlineData("gzip")]
    [InlineData("zlib")]
    public void Compressed_base64_data_is_inflated(string compression)
    {
        var raw = new byte[12 * 4];
        BitConverter.GetBytes(3u).CopyTo(raw, 7 * 4);
        using var packed = new MemoryStream();
        using (Stream stream = compression == "gzip" ? new GZipStream(packed, CompressionLevel.Optimal, true) : new ZLibStream(packed, CompressionLevel.Optimal, true))
            stream.Write(raw);
        string layer = $$"""{ "type": "tilelayer", "name": "g", "width": 4, "height": 3, "encoding": "base64", "compression": "{{compression}}", "data": "{{Convert.ToBase64String(packed.ToArray())}}" }""";

        var cells = TiledReader.Read(Map(layer)).TileLayers[0].Cells;

        Assert.Equal(new TiledCell(3, 1, 3), Assert.Single(cells));
    }

    [Fact]
    public void Infinite_maps_are_read_from_their_chunks_including_negative_positions()
    {
        string layer = """
            { "type": "tilelayer", "name": "g", "chunks": [
                { "x": -2, "y": -1, "width": 2, "height": 2, "data": [1, 0, 0, 2] },
                { "x": 0, "y": 0, "width": 2, "height": 2, "data": [0, 3, 0, 0] } ] }
            """;
        string json = Map(layer, extra: "").Replace("\"infinite\": false", "\"infinite\": true");

        var map = TiledReader.Read(json);

        Assert.True(map.Infinite);
        Assert.Equal(
            [new TiledCell(-2, -1, 1), new TiledCell(-1, 0, 2), new TiledCell(1, 0, 3)],
            map.TileLayers[0].Cells);
    }

    [Fact]
    public void Group_layers_are_flattened_with_their_offsets_added()
    {
        string group = """
            { "type": "group", "name": "g", "offsetx": 10, "layers": [
                { "type": "tilelayer", "name": "inner", "width": 4, "height": 3, "offsetx": 5, "data": [1,0,0,0, 0,0,0,0, 0,0,0,0] } ] }
            """;

        var read = TiledReader.Read(Map(group)).TileLayers;

        var inner = Assert.Single(read);
        Assert.Equal("inner", inner.Name);
        Assert.Equal(15f, inner.OffsetX);
    }

    [Fact]
    public void Object_layers_read_rectangles_points_and_tile_objects_with_properties()
    {
        string objects = """
            { "type": "objectgroup", "name": "things", "objects": [
                { "id": 1, "name": "start", "type": "spawn", "x": 8, "y": 40, "width": 0, "height": 0, "point": true,
                  "properties": [ { "name": "who", "type": "string", "value": "hero" } ] },
                { "id": 2, "name": "zone", "class": "trigger", "x": 16, "y": 16, "width": 32, "height": 16 },
                { "id": 3, "name": "flag", "gid": 7, "x": 0, "y": 16, "width": 16, "height": 16 } ] }
            """;

        var layer = TiledReader.Read(Map(GroundLayer() + "," + objects)).ObjectLayers[0];

        Assert.Equal(3, layer.Objects.Count);
        Assert.True(layer.Objects[0].IsPoint);
        Assert.Equal("spawn", layer.Objects[0].Class);
        Assert.Equal("hero", layer.Objects[0].Properties.GetString("who"));
        Assert.Equal("trigger", layer.Objects[1].Class);
        Assert.Equal((32f, 16f), (layer.Objects[1].Width, layer.Objects[1].Height));
        Assert.Equal(7u, layer.Objects[2].Gid);
    }

    [Fact]
    public void External_tile_sets_are_read_through_the_callback_with_the_maps_first_gid()
    {
        string json = Map(GroundLayer(), tileSets: """{ "firstgid": 5, "source": "tiles.tsj" }""");

        var map = TiledReader.Read(json, source => source == "tiles.tsj" ? TileSetJson.Replace("\"firstgid\": 1, ", "") : throw new FileNotFoundException(source));

        var tileSet = Assert.Single(map.TileSets);
        Assert.Equal(5, tileSet.FirstGid);
        Assert.Equal("tiles", tileSet.Name);
        Assert.Equal(2, tileSet.Tiles.Count);
    }

    [Fact]
    public void An_external_tile_set_with_no_way_to_read_it_is_an_error()
    {
        string json = Map(GroundLayer(), tileSets: """{ "firstgid": 1, "source": "tiles.tsj" }""");

        var ex = Assert.Throws<InvalidOperationException>(() => TiledReader.Read(json));

        Assert.Contains("tiles.tsj", ex.Message);
    }

    [Fact]
    public void Reading_a_file_finds_external_tile_sets_next_to_it()
    {
        string folder = Path.Combine(Path.GetTempPath(), "csharpgame-tiled-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "tiles.tsj"), TileSetJson.Replace("\"firstgid\": 1, ", ""));
            File.WriteAllText(Path.Combine(folder, "level.tmj"), Map(GroundLayer(), tileSets: """{ "firstgid": 1, "source": "tiles.tsj" }"""));

            var map = TiledReader.ReadFile(Path.Combine(folder, "level.tmj"));

            Assert.Equal("tiles", map.TileSets[0].Name);
            Assert.Throws<FileNotFoundException>(() => TiledReader.ReadFile(Path.Combine(folder, "missing.tmj")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("orientation", "isometric")]
    public void Non_orthogonal_maps_are_rejected(string key, string value)
    {
        string json = Map(GroundLayer()).Replace($"\"{key}\": \"orthogonal\"", $"\"{key}\": \"{value}\"");

        var ex = Assert.Throws<InvalidOperationException>(() => TiledReader.Read(json));

        Assert.Contains(value, ex.Message);
    }

    [Fact]
    public void A_tile_set_that_is_a_collection_of_images_is_rejected()
    {
        string json = Map(GroundLayer(), tileSets: """{ "firstgid": 1, "name": "props", "tilewidth": 16, "tileheight": 16, "tilecount": 2, "columns": 0 }""");

        var ex = Assert.Throws<InvalidOperationException>(() => TiledReader.Read(json));

        Assert.Contains("collections of images", ex.Message);
    }

    [Fact]
    public void Unknown_compression_and_text_encodings_are_rejected()
    {
        string zstd = """{ "type": "tilelayer", "name": "g", "width": 4, "height": 3, "encoding": "base64", "compression": "zstd", "data": "AAAA" }""";
        string xml = """{ "type": "tilelayer", "name": "g", "width": 4, "height": 3, "encoding": "xml", "data": "AAAA" }""";

        Assert.Contains("zstd", Assert.Throws<InvalidOperationException>(() => TiledReader.Read(Map(zstd))).Message);
        Assert.Contains("xml", Assert.Throws<InvalidOperationException>(() => TiledReader.Read(Map(xml))).Message);
    }

    [Fact]
    public void Invalid_json_is_reported_as_a_map_error()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TiledReader.Read("{ nope"));

        Assert.Contains("not valid JSON", ex.Message);
    }

    [Fact]
    public void Property_getters_fail_loudly_on_the_wrong_type()
    {
        var props = new TiledProperties(new Dictionary<string, string> { ["a"] = "yes", ["b"] = "1.5" });

        Assert.Throws<InvalidOperationException>(() => props.GetBool("a"));
        Assert.Equal(1.5f, props.GetFloat("b"));
        Assert.Throws<InvalidOperationException>(() => props.GetFloat("a"));
        Assert.False(props.GetBool("missing"));
        Assert.Equal("fallback", props.GetString("missing", "fallback"));
    }

    // ---- converting ----

    [Fact]
    public void Rows_are_flipped_so_the_map_is_upright_and_ids_become_one_based()
    {
        var imported = TiledConverter.Convert(TiledReader.Read(Map(GroundLayer())));

        var layer = Assert.Single(imported.Layers).Layer;
        // The bottom row of the file ("1,1,1,1") is row 0 of the engine layer; Tiled tile 0 is engine tile 1.
        Assert.Equal(1, TileId.Id(layer.GetTile(0, 0)));      // global id 1 is local tile 0, engine id 1
        Assert.Equal(2, TileId.Id(layer.GetTile(1, 1)));      // global id 2: local 1, engine 2
        Assert.Equal(0, layer.GetTile(1, 2));                 // the top row of the file is empty
    }

    [Fact]
    public void Flip_bits_become_tile_flags_and_the_id_is_cleaned()
    {
        string layer = GroundLayer($"[{FlipH | 2u}, {FlipV | 2u}, {FlipD | 2u}, {FlipH | FlipV | FlipD | 2u}, 0,0,0,0, 0,0,0,0]");

        var map = TiledConverter.Convert(TiledReader.Read(Map(layer)));

        var tiles = map.Layers[0].Layer;
        Assert.Equal(TileId.With(2, TileId.FlipHorizontal), tiles.GetTile(0, 2));
        Assert.Equal(TileId.With(2, TileId.FlipVertical), tiles.GetTile(1, 2));
        Assert.Equal(TileId.With(2, TileId.FlipDiagonal), tiles.GetTile(2, 2));
        Assert.Equal(TileId.With(2, TileId.FlipHorizontal | TileId.FlipVertical | TileId.FlipDiagonal), tiles.GetTile(3, 2));
    }

    [Fact]
    public void The_tile_set_gets_solid_tiles_and_animations_converted_to_engine_numbering()
    {
        var imported = TiledConverter.Convert(TiledReader.Read(Map(GroundLayer())), new TiledImportOptions { TextureFolder = "maps/" });

        var set = Assert.Single(imported.TileSets);
        Assert.Equal("maps/tiles.png", set.TextureName);
        Assert.Equal([1], set.Data.Solid);
        var animation = Assert.Single(set.Data.Animations);
        Assert.Equal(5, animation.Tile);
        Assert.Equal([(5, 0.2f), (6, 0.3f)], animation.Frames.Select(f => (f.Tile, f.Duration)));
        Assert.Equal(5, set.Data.Columns);
        Assert.Equal(2, set.Data.Rows);
    }

    [Fact]
    public void The_converted_tile_set_data_builds_a_working_tile_set()
    {
        var imported = TiledConverter.Convert(TiledReader.Read(Map(GroundLayer())));

        var tileSet = imported.TileSets[0].Data.CreateTileSet(Engine.Core.Assets.AssetHandle<Engine.Rendering.Texture2D>.Invalid, 80, 32);

        Assert.True(tileSet.IsSolid(1));
        Assert.True(tileSet.IsAnimated(5));
        Assert.Equal(6, tileSet.ResolveFrame(5, 0.3));
    }

    [Fact]
    public void Positions_are_scaled_and_flipped_to_world_space_with_y_up()
    {
        string objects = """{ "type": "objectgroup", "name": "o", "objects": [ { "id": 1, "name": "p", "x": 8, "y": 40, "point": true } ] }""";
        var map = TiledReader.Read(Map(GroundLayer() + "," + objects));

        var imported = TiledConverter.Convert(map, new TiledImportOptions { WorldUnitsPerPixel = 2f });

        // The map is 48 px tall; the point is 8 px down from the top, so 40 px up from the bottom... that is (48 - 40) = 8 px, doubled.
        Assert.Equal(new Vector2(16f, 16f), imported.Objects[0].Position);
        Assert.Equal(new Vector2(4 * 32f, 3 * 32f), imported.WorldSize);
        Assert.Equal(32f, imported.TileSize);
    }

    [Fact]
    public void Layer_offsets_and_parallax_are_converted_to_engine_conventions()
    {
        string layer = """
            { "type": "tilelayer", "name": "far", "width": 4, "height": 3, "data": [1,0,0,0, 0,0,0,0, 0,0,0,0],
              "offsetx": 8, "offsety": 4, "parallaxx": 0.25, "parallaxy": 1,
              "properties": [ { "name": "foreground", "type": "bool", "value": true } ] }
            """;

        var imported = TiledConverter.Convert(TiledReader.Read(Map(layer)), new TiledImportOptions { WorldUnitsPerPixel = 2f });

        var converted = Assert.Single(imported.Layers);
        Assert.Equal(new Vector2(16f, -8f), converted.Position);    // Tiled's y points down, ours up
        Assert.Equal(new Vector2(0.75f, 0f), converted.Parallax);   // Tiled 1 = normal = our 0
        Assert.True(converted.Foreground);
    }

    [Fact]
    public void A_layer_using_two_tile_sets_is_split_in_one_layer_per_set()
    {
        string two = """
            [ { "firstgid": 1, "name": "a", "tilewidth": 16, "tileheight": 16, "image": "a.png", "imagewidth": 32, "imageheight": 16, "columns": 2, "tilecount": 2 },
              { "firstgid": 3, "name": "b", "tilewidth": 16, "tileheight": 16, "image": "b.png", "imagewidth": 32, "imageheight": 16, "columns": 2, "tilecount": 2 } ]
            """;
        string json = Map(GroundLayer("[1,3,0,0, 0,0,0,0, 0,0,0,0]")).Replace(Map(GroundLayer()).Split("\"tilesets\": [ ")[1].Split(" ],\n")[0], two.Trim('[', ']', ' ', '\n'));

        var imported = TiledConverter.Convert(TiledReader.Read(json));

        Assert.Equal(2, imported.Layers.Count);
        Assert.Equal(["ground#a", "ground#b"], imported.Layers.Select(l => l.Name));
        Assert.Equal(["a", "b"], imported.Layers.Select(l => l.TileSetName));
        Assert.Equal(1, TileId.Id(imported.Layers[1].Layer.GetTile(1, 2))); // global 3 is the first tile of set b
    }

    [Fact]
    public void A_tile_value_no_tile_set_owns_is_reported_with_its_layer_and_cell()
    {
        string layer = GroundLayer("[99,0,0,0, 0,0,0,0, 0,0,0,0]");

        var ex = Assert.Throws<InvalidOperationException>(() => TiledConverter.Convert(TiledReader.Read(Map(layer))));

        Assert.Contains("ground", ex.Message);
        Assert.Contains("tile set", ex.Message);
    }

    [Fact]
    public void Non_square_tiles_are_rejected()
    {
        string json = Map(GroundLayer()).Replace("\"tileheight\": 16, \"infinite\"", "\"tileheight\": 24, \"infinite\"");

        Assert.Throws<InvalidOperationException>(() => TiledConverter.Convert(TiledReader.Read(json)));
    }

    [Fact]
    public void Empty_layers_produce_no_engine_layers()
    {
        var imported = TiledConverter.Convert(TiledReader.Read(Map(GroundLayer("[0,0,0,0, 0,0,0,0, 0,0,0,0]"))));

        Assert.Empty(imported.Layers);
    }
}
