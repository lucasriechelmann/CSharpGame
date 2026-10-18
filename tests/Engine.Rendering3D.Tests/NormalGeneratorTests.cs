using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class NormalGeneratorTests
{
    [Fact]
    public void A_single_triangle_gets_its_face_normal_on_every_vertex()
    {
        Vector3[] positions = [new(0f, 0f, 0f), new(1f, 0f, 0f), new(0f, 1f, 0f)];

        var normals = NormalGenerator.Smooth(positions, [0u, 1u, 2u]);

        Assert.All(normals, n => Assert.Equal(Vector3.UnitZ, n));
    }

    [Fact]
    public void Winding_decides_which_way_the_normal_points()
    {
        Vector3[] positions = [new(0f, 0f, 0f), new(1f, 0f, 0f), new(0f, 1f, 0f)];

        var normals = NormalGenerator.Smooth(positions, [0u, 2u, 1u]);

        Assert.Equal(-Vector3.UnitZ, normals[0]);
    }

    [Fact]
    public void A_shared_edge_vertex_gets_the_average_of_both_faces()
    {
        // Two triangles folded 90 degrees along the Y axis edge (vertices 0 and 1): one faces +Z, the other +X.
        Vector3[] positions = [new(0f, 0f, 0f), new(0f, 1f, 0f), new(1f, 0f, 0f), new(0f, 0f, 1f)];
        uint[] indices = [0, 2, 1, 0, 1, 3];

        var normals = NormalGenerator.Smooth(positions, indices);

        var expected = Vector3.Normalize(new Vector3(1f, 0f, 1f));
        Assert.Equal(expected.X, normals[0].X, 4);
        Assert.Equal(expected.Z, normals[0].Z, 4);
        Assert.Equal(Vector3.UnitZ, normals[2]); // only the first face touches vertex 2
    }

    [Fact]
    public void An_unused_vertex_gets_up()
    {
        Vector3[] positions = [new(0f, 0f, 0f), new(1f, 0f, 0f), new(0f, 1f, 0f), new(5f, 5f, 5f)];

        var normals = NormalGenerator.Smooth(positions, [0u, 1u, 2u]);

        Assert.Equal(Vector3.UnitY, normals[3]);
    }
}
