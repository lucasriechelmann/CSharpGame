using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class MeshBuilderTests
{
    /// <summary>A triangle is front-facing when its geometric normal (from the winding) points the same way as its vertex normals.</summary>
    private static void AssertOutwardWinding(MeshData mesh, Func<Vector3, Vector3, bool> pointsOutward)
    {
        for (int t = 0; t < mesh.TriangleCount; t++)
        {
            var a = mesh.Vertices[mesh.Indices[t * 3]];
            var b = mesh.Vertices[mesh.Indices[t * 3 + 1]];
            var c = mesh.Vertices[mesh.Indices[t * 3 + 2]];
            var geometric = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
            Assert.True(geometric.LengthSquared() > 1e-10f, $"triangle {t} is degenerate");
            Assert.True(Vector3.Dot(Vector3.Normalize(geometric), a.Normal) > 0.5f, $"triangle {t} winds against its normal");
            Assert.True(pointsOutward(a.Position, a.Normal), $"triangle {t} normal points inwards");
        }
    }

    [Fact]
    public void Cube_has_24_vertices_and_12_triangles()
    {
        var cube = MeshBuilder.Cube(new Vector3(2f, 4f, 6f));

        Assert.Equal(24, cube.Vertices.Length);
        Assert.Equal(12, cube.TriangleCount);
    }

    [Fact]
    public void Cube_bounds_match_its_size()
    {
        var cube = MeshBuilder.Cube(new Vector3(2f, 4f, 6f));

        Assert.Equal(new Vector3(-1f, -2f, -3f), cube.BoundsMin);
        Assert.Equal(new Vector3(1f, 2f, 3f), cube.BoundsMax);
        Assert.Equal(Vector3.Zero, cube.BoundsCenter);
        Assert.Equal(MathF.Sqrt(1f + 4f + 9f), cube.BoundsRadius, 4);
    }

    [Fact]
    public void Cube_faces_wind_counter_clockwise_seen_from_outside()
    {
        var cube = MeshBuilder.Cube(Vector3.One);

        AssertOutwardWinding(cube, (position, normal) => Vector3.Dot(position, normal) > 0f);
    }

    [Fact]
    public void Cube_normals_are_unit_length()
    {
        foreach (var vertex in MeshBuilder.Cube(Vector3.One).Vertices)
            Assert.Equal(1f, vertex.Normal.Length(), 5);
    }

    [Fact]
    public void Cube_rejects_a_non_positive_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MeshBuilder.Cube(new Vector3(1f, 0f, 1f)));
    }

    [Fact]
    public void Plane_faces_up_and_lies_flat()
    {
        var plane = MeshBuilder.Plane(10f, 4f);

        Assert.Equal(2, plane.TriangleCount);
        Assert.All(plane.Vertices, v =>
        {
            Assert.Equal(0f, v.Position.Y);
            Assert.Equal(Vector3.UnitY, v.Normal);
        });
        AssertOutwardWinding(plane, (_, normal) => normal.Y > 0.5f);
    }

    [Fact]
    public void Plane_uv_repeat_scales_the_texture_coordinates()
    {
        var plane = MeshBuilder.Plane(1f, 1f, uvRepeat: 4f);

        Assert.Equal(4f, plane.Vertices.Max(v => v.Uv.X));
        Assert.Equal(4f, plane.Vertices.Max(v => v.Uv.Y));
    }

    [Fact]
    public void Sphere_vertices_lie_on_the_radius_with_outward_normals()
    {
        var sphere = MeshBuilder.Sphere(2.5f, segments: 12, rings: 6);

        foreach (var vertex in sphere.Vertices)
        {
            Assert.Equal(2.5f, vertex.Position.Length(), 4);
            Assert.Equal(1f, vertex.Normal.Length(), 4);
            Assert.True(Vector3.Dot(Vector3.Normalize(vertex.Position), vertex.Normal) > 0.999f);
        }
    }

    [Fact]
    public void Sphere_triangles_wind_counter_clockwise_seen_from_outside()
    {
        var sphere = MeshBuilder.Sphere(1f, segments: 12, rings: 6);

        // Only triangles with real area are checked: the pole fans produce none, but the helper also rejects degenerate ones.
        AssertOutwardWinding(sphere, (position, normal) => Vector3.Dot(position, normal) > 0f);
    }

    [Fact]
    public void Sphere_triangle_count_drops_the_collapsed_pole_quads()
    {
        // segments * rings quads, two triangles each, minus one triangle per segment at each pole.
        var sphere = MeshBuilder.Sphere(1f, segments: 8, rings: 4);

        Assert.Equal(8 * 4 * 2 - 2 * 8, sphere.TriangleCount);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(8, 1)]
    public void Sphere_rejects_too_coarse_a_grid(int segments, int rings)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MeshBuilder.Sphere(1f, segments, rings));
    }
}
