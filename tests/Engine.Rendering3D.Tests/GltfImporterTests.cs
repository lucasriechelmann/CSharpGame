using System.Numerics;
using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Rendering3D.Gltf;
using Engine.Rendering3D.Tests.Fixtures;

namespace Engine.Rendering3D.Tests;

public class GltfImporterTests
{
    [Fact]
    public void A_triangle_imports_with_its_positions_normals_and_indices()
    {
        var model = GltfBytes.Import(GltfBytes.TriangleJson());

        var part = Assert.Single(model.Parts);
        Assert.Equal(3, part.Mesh.Vertices.Length);
        Assert.Equal(new Vector3(1f, 0f, 0f), part.Mesh.Vertices[1].Position);
        Assert.Equal(Vector3.UnitZ, part.Mesh.Vertices[0].Normal);
        Assert.Equal([0u, 1u, 2u], part.Mesh.Indices);
    }

    [Fact]
    public void A_primitive_without_indices_draws_its_vertices_in_order()
    {
        var model = GltfBytes.Import(GltfBytes.TriangleJson(indexed: false));

        Assert.Equal([0u, 1u, 2u], model.Parts[0].Mesh.Indices);
    }

    [Fact]
    public void Missing_normals_are_generated_from_the_triangle()
    {
        var model = GltfBytes.Import(GltfBytes.TriangleJson(withNormals: false));

        // The triangle (0,0,0), (1,0,0), (0,1,0) winds counter-clockwise seen from +Z.
        Assert.All(model.Parts[0].Mesh.Vertices, v => Assert.Equal(Vector3.UnitZ, v.Normal));
    }

    [Fact]
    public void Primitive_without_a_material_gets_the_default_white_one()
    {
        var model = GltfBytes.Import(GltfBytes.TriangleJson());

        var material = model.Materials[model.Parts[0].MaterialIndex];
        Assert.Equal(Color.White, material.BaseColor);
        Assert.Null(material.Image);
    }

    [Fact]
    public void Material_colour_and_double_sided_are_read()
    {
        string materials = """
            "materials": [ { "pbrMetallicRoughness": { "baseColorFactor": [0.5, 0.25, 0.125, 1], "roughnessFactor": 0.2, "metallicFactor": 0 }, "doubleSided": true } ],
            """;

        var model = GltfBytes.Import(GltfBytes.TriangleJson(materialsJson: materials));

        var material = model.Materials[model.Parts[0].MaterialIndex];
        Assert.Equal(new Color(0.5f, 0.25f, 0.125f, 1f), material.BaseColor);
        Assert.True(material.DoubleSided);
    }


    [Fact]
    public void Node_translation_rotation_and_scale_compose_as_scale_rotate_translate()
    {
        // Quarter turn around Y: +X becomes -Z.
        float half = MathF.Sqrt(0.5f);
        var model = GltfBytes.Import(GltfBytes.TriangleJson("\"translation\": [10, 0, 0], \"rotation\": [0, " + half.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ", 0, " + half.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "], \"scale\": [2, 1, 1]"));

        var world = GltfBytes.Transform(model, Vector3.UnitX);

        Assert.Equal(10f, world.X, 4);
        Assert.Equal(-2f, world.Z, 4);
    }

    [Fact]
    public void Node_matrix_is_read_column_major()
    {
        // Column-major translation by (1, 2, 3): the last column holds the offset, i.e. entries 12..14.
        var model = GltfBytes.Import(GltfBytes.TriangleJson("\"matrix\": [1,0,0,0, 0,1,0,0, 0,0,1,0, 1,2,3,1]"));

        Assert.Equal(new Vector3(1f, 2f, 3f), GltfBytes.Transform(model, Vector3.Zero));
    }

    [Fact]
    public void Child_nodes_inherit_their_parents_transform()
    {
        string json = """
            {
              "asset": { "version": "2.0" },
              "scenes": [ { "nodes": [0] } ],
              "nodes": [
                { "translation": [10, 0, 0], "children": [1] },
                { "mesh": 0, "translation": [0, 5, 0] }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 } } ] } ],
              "accessors": [ { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" } ],
              "bufferViews": [ { "buffer": 0, "byteLength": 36 } ],
              "buffers": [ { "byteLength": 36, "uri": "BUFFER" } ]
            }
            """.Replace("BUFFER", GltfBytes.DataUri(GltfBytes.Floats(0, 0, 0, 1, 0, 0, 0, 1, 0)));

        var model = GltfBytes.Import(json);

        Assert.Equal(new Vector3(10f, 5f, 0f), GltfBytes.Transform(model, Vector3.Zero));
    }

    [Fact]
    public void A_scene_less_file_shows_every_root_node()
    {
        string json = GltfBytes.TriangleJson().Replace("\"scenes\": [ { \"nodes\": [0] } ],", "").Replace("\"scene\": 0,", "");

        var model = GltfBytes.Import(json);

        Assert.Single(model.Parts);
    }

    [Fact]
    public void Non_triangle_primitive_modes_are_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(GltfBytes.TriangleJson(primitiveExtra: ", \"mode\": 1")));

        Assert.Contains("mode 1", ex.Message);
    }

    [Fact]
    public void Required_extensions_fail_the_import()
    {
        string json = GltfBytes.TriangleJson().Replace("\"scene\": 0,", "\"scene\": 0, \"extensionsRequired\": [\"KHR_draco_mesh_compression\"],");

        var ex = Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));

        Assert.Contains("KHR_draco_mesh_compression", ex.Message);
    }

    [Fact]
    public void Only_gltf_two_is_accepted()
    {
        string json = GltfBytes.TriangleJson().Replace("\"version\": \"2.0\"", "\"version\": \"1.0\"");

        Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));
    }

    [Fact]
    public void Invalid_json_is_reported_as_a_gltf_error()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => GltfBytes.Import("{ nope"));

        Assert.Contains("glTF JSON is not valid", ex.Message);
    }

    [Fact]
    public void A_primitive_without_positions_is_rejected()
    {
        string json = GltfBytes.TriangleJson().Replace("\"POSITION\":0,", "");

        Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));
    }

    [Fact]
    public void Unknown_material_index_is_rejected()
    {
        string json = GltfBytes.TriangleJson().Replace("\"indices\":2", "\"indices\":2, \"material\": 4");

        var ex = Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));

        Assert.Contains("material 4", ex.Message);
    }

    [Fact]
    public void A_node_cycle_is_reported_not_followed_forever()
    {
        string json = """
            {
              "asset": { "version": "2.0" },
              "scenes": [ { "nodes": [0] } ],
              "nodes": [ { "children": [1] }, { "children": [0] } ]
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));

        Assert.Contains("deeper than", ex.Message);
    }

    [Fact]
    public void External_buffers_resolve_next_to_the_file_and_missing_ones_are_reported()
    {
        string folder = Path.Combine(Path.GetTempPath(), "csharpgame-gltf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string inline = GltfBytes.TriangleJson();
            // Move the data URI out into a .bin file and refer to it by name.
            int start = inline.IndexOf("data:", StringComparison.Ordinal);
            int end = inline.IndexOf('"', start);
            byte[] data = Convert.FromBase64String(inline[(inline.IndexOf(',', start) + 1)..end]);
            File.WriteAllBytes(Path.Combine(folder, "tri.bin"), data);
            string path = Path.Combine(folder, "tri.gltf");
            File.WriteAllText(path, inline[..start] + "tri.bin" + inline[end..]);

            var model = GltfImporter.ImportFile(path);
            Assert.Equal(3, model.Parts[0].Mesh.Vertices.Length);

            File.Delete(Path.Combine(folder, "tri.bin"));
            Assert.Throws<FileNotFoundException>(() => GltfImporter.ImportFile(path));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Writer_output_imports_back_unchanged()
    {
        var cube = MeshBuilder.Cube(new Vector3(1f, 2f, 3f));
        var sphere = MeshBuilder.Sphere(0.5f, 8, 4);
        var placed = Matrix4x4.CreateRotationY(0.5f) * Matrix4x4.CreateTranslation(1f, 2f, 3f);
        var glb = GltfWriter.WriteGlb(
        [
            new GltfExportPart("body", cube, new Color(0.9f, 0.1f, 0.1f), Matrix4x4.Identity),
            new GltfExportPart("ball", sphere, new Color(0.1f, 0.1f, 0.9f), placed),
        ]);

        var model = GltfImporter.Import(GltfContainer.Read(glb), null);

        Assert.Equal(2, model.Parts.Count);
        Assert.Equal(2, model.Materials.Count);
        var body = model.Parts[0];
        Assert.Equal(cube.Vertices.Length, body.Mesh.Vertices.Length);
        Assert.Equal(cube.Indices, body.Mesh.Indices);
        Assert.Equal(cube.BoundsMin, body.Mesh.BoundsMin);
        Assert.Equal(cube.BoundsMax, body.Mesh.BoundsMax);
        Assert.Equal(cube.Vertices[7].Uv, body.Mesh.Vertices[7].Uv);
        Assert.Equal(new Color(0.9f, 0.1f, 0.1f), model.Materials[body.MaterialIndex].BaseColor);
        Assert.Equal(placed, model.Parts[1].Local);
        Assert.Equal(sphere.Indices, model.Parts[1].Mesh.Indices);
        Assert.Equal(new Color(0.1f, 0.1f, 0.9f), model.Materials[model.Parts[1].MaterialIndex].BaseColor);
    }

    [Fact]
    public void Parts_with_the_same_colour_share_a_material()
    {
        var cube = MeshBuilder.Cube(Vector3.One);
        var glb = GltfWriter.WriteGlb(
        [
            new GltfExportPart("a", cube, Color.Red, Matrix4x4.Identity),
            new GltfExportPart("b", cube, Color.Red, Matrix4x4.CreateTranslation(2f, 0f, 0f)),
        ]);

        var model = GltfImporter.Import(GltfContainer.Read(glb), null);

        Assert.Single(model.Materials);
        Assert.Equal(model.Parts[0].MaterialIndex, model.Parts[1].MaterialIndex);
    }

    [Fact]
    public void Writing_no_parts_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => GltfWriter.WriteGlb([]));
    }

    [Fact]
    public void Embedded_images_are_returned_as_bytes()
    {
        // The importer does not decode images (that needs GL); it hands the encoded bytes to the GPU model.
        byte[] fakePng = [137, 80, 78, 71, 1, 2, 3, 4];
        string materials = """
            "materials": [ { "pbrMetallicRoughness": { "baseColorTexture": { "index": 0 } } } ],
            "textures": [ { "source": 0 } ],
            "images": [ { "uri": "IMAGE" } ],
            """.Replace("IMAGE", "data:image/png;base64," + Convert.ToBase64String(fakePng));

        var model = GltfBytes.Import(GltfBytes.TriangleJson(materialsJson: materials));

        Assert.Equal(fakePng, model.Materials[0].Image);
    }
}
