using System.Numerics;
using Engine.Rendering3D.Tests.Fixtures;

namespace Engine.Rendering3D.Tests;

public class ModelNodeTests
{
    private const string TwoLevelTree = """
        {
          "asset": { "version": "2.0" },
          "scenes": [ { "nodes": [0] } ],
          "nodes": [
            { "name": "root", "translation": [10, 0, 0], "children": [1, 2] },
            { "name": "arm", "mesh": 0, "translation": [0, 5, 0] },
            { "name": "leg", "mesh": 0, "translation": [0, -5, 0] }
          ],
          "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 } } ] } ],
          "accessors": [ { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" } ],
          "bufferViews": [ { "buffer": 0, "byteLength": 36 } ],
          "buffers": [ { "byteLength": 36, "uri": "BUFFER" } ]
        }
        """;

    private static ModelData Import() =>
        GltfBytes.Import(TwoLevelTree.Replace("BUFFER", GltfBytes.DataUri(GltfBytes.Floats(0, 0, 0, 1, 0, 0, 0, 1, 0))));

    [Fact]
    public void The_node_tree_is_kept_with_names_and_parents_before_children()
    {
        var model = Import();

        Assert.Equal(["root", "arm", "leg"], model.Nodes.Select(n => n.Name));
        Assert.Equal([-1, 0, 0], model.Nodes.Select(n => n.Parent));
    }

    [Fact]
    public void Each_part_points_at_its_node()
    {
        var model = Import();

        Assert.Equal([1, 2], model.Parts.Select(p => p.Node));
    }

    [Fact]
    public void Node_local_matrices_are_the_files_own_not_the_flattened_ones()
    {
        var model = Import();

        Assert.Equal(new Vector3(0, 5, 0), model.Nodes[1].Local.Translation);
        Assert.Equal(new Vector3(10, 5, 0), model.Parts[0].Local.Translation);
    }

    [Fact]
    public void Rest_pose_world_matrices_equal_the_flattened_part_matrices()
    {
        var model = Import();
        var world = new Matrix4x4[model.Nodes.Count];

        ModelPose.ComputeRestWorld(model.Nodes, world);

        foreach (var part in model.Parts) Assert.Equal(part.Local, world[part.Node]);
    }

    [Fact]
    public void A_posed_node_moves_its_children_too()
    {
        var model = Import();
        var locals = model.Nodes.Select(n => n.Local).ToArray();
        locals[0] = Matrix4x4.CreateTranslation(100, 0, 0); // an animation moved the root
        var world = new Matrix4x4[model.Nodes.Count];

        ModelPose.ComputeWorld(model.Nodes, locals, world);

        Assert.Equal(new Vector3(100, 5, 0), world[1].Translation);
        Assert.Equal(new Vector3(100, -5, 0), world[2].Translation);
    }

    [Fact]
    public void A_parent_listed_after_its_child_is_refused()
    {
        var nodes = new[] { new ModelNodeData("a", 1, Matrix4x4.Identity), new ModelNodeData("b", -1, Matrix4x4.Identity) };

        Assert.Throws<ArgumentException>(() => ModelPose.ComputeRestWorld(nodes, new Matrix4x4[2]));
    }

    [Fact]
    public void A_wrong_sized_span_is_refused()
    {
        var model = Import();

        Assert.Throws<ArgumentException>(() => ModelPose.ComputeRestWorld(model.Nodes, new Matrix4x4[1]));
    }

    [Fact]
    public void Hand_built_model_data_has_no_nodes()
    {
        var data = new ModelData([], []);

        Assert.Empty(data.Nodes);
    }
}
