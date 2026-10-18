using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class TangentGeneratorTests
{
    /// <summary>A quad in the XY plane facing +Z whose U grows with X and whose V grows downward (image rows), like a glTF plane.</summary>
    private static (Vertex3D[] Vertices, uint[] Indices) Quad(bool mirrored = false)
    {
        var normal = Vector3.UnitZ;
        float u0 = mirrored ? 1f : 0f, u1 = mirrored ? 0f : 1f;
        var vertices = new[]
        {
            new Vertex3D(new Vector3(0, 0, 0), normal, new Vector2(u0, 1)), // bottom left: V = 1, the bottom of the image
            new Vertex3D(new Vector3(1, 0, 0), normal, new Vector2(u1, 1)),
            new Vertex3D(new Vector3(1, 1, 0), normal, new Vector2(u1, 0)),
            new Vertex3D(new Vector3(0, 1, 0), normal, new Vector2(u0, 0)),
        };
        return (vertices, [0, 1, 2, 0, 2, 3]);
    }

    [Fact]
    public void The_tangent_points_along_the_direction_u_grows()
    {
        var (vertices, indices) = Quad();

        TangentGenerator.Generate(vertices, indices);

        foreach (var vertex in vertices)
        {
            Assert.Equal(1f, vertex.Tangent.X, 4);
            Assert.Equal(0f, vertex.Tangent.Y, 4);
            Assert.Equal(0f, vertex.Tangent.Z, 4);
        }
    }

    [Fact]
    public void An_ordinary_mapping_is_right_handed_and_the_bitangent_points_up_the_image()
    {
        var (vertices, indices) = Quad();

        TangentGenerator.Generate(vertices, indices);

        var v = vertices[0];
        var bitangent = Vector3.Cross(v.Normal, new Vector3(v.Tangent.X, v.Tangent.Y, v.Tangent.Z)) * v.Tangent.W;
        Assert.Equal(1f, v.Tangent.W);
        Assert.Equal(1f, bitangent.Y, 4); // up the image is +Y here
    }

    [Fact]
    public void A_mirrored_mapping_is_left_handed()
    {
        var (vertices, indices) = Quad(mirrored: true);

        TangentGenerator.Generate(vertices, indices);

        Assert.Equal(-1f, vertices[0].Tangent.W);
        Assert.Equal(-1f, vertices[0].Tangent.X, 4); // u now grows towards -X
    }

    [Fact]
    public void Tangents_are_unit_length_and_perpendicular_to_the_normal()
    {
        var sphere = MeshBuilder.Sphere(1f, 12, 6);

        foreach (var vertex in sphere.Vertices)
        {
            var t = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
            Assert.Equal(1f, t.Length(), 3);
            Assert.Equal(0f, Vector3.Dot(t, vertex.Normal), 3);
            Assert.True(vertex.Tangent.W is 1f or -1f);
        }
    }

    [Fact]
    public void Every_builder_shape_comes_with_tangents()
    {
        foreach (var mesh in new[] { MeshBuilder.Cube(Vector3.One), MeshBuilder.Plane(2f, 2f), MeshBuilder.Sphere(1f), MeshBuilder.Capsule(0.5f, 2f) })
            Assert.True(TangentGenerator.HasTangents(mesh.Vertices));
    }

    [Fact]
    public void Triangles_with_no_uv_area_still_get_a_usable_tangent_instead_of_nan()
    {
        var normal = Vector3.UnitY;
        var vertices = new[]
        {
            new Vertex3D(new Vector3(0, 0, 0), normal, Vector2.Zero),
            new Vertex3D(new Vector3(1, 0, 0), normal, Vector2.Zero),
            new Vertex3D(new Vector3(0, 0, 1), normal, Vector2.Zero),
        };

        TangentGenerator.Generate(vertices, [0, 1, 2]);

        foreach (var vertex in vertices)
        {
            var t = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
            Assert.False(float.IsNaN(t.X));
            Assert.Equal(1f, t.Length(), 3);
            Assert.Equal(0f, Vector3.Dot(t, normal), 3);
        }
    }

    [Fact]
    public void Tangents_a_builder_supplies_are_kept()
    {
        var vertices = new[]
        {
            new Vertex3D(Vector3.Zero, Vector3.UnitZ, Vector2.Zero, new Vector4(0, 1, 0, 1)),
            new Vertex3D(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX, new Vector4(0, 1, 0, 1)),
            new Vertex3D(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY, new Vector4(0, 1, 0, 1)),
        };

        var data = new MeshData(vertices, [0, 1, 2]);

        Assert.Equal(new Vector4(0, 1, 0, 1), data.Vertices[0].Tangent);
    }

}
