using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class MeshDataTests
{
    private static Vertex3D At(float x, float y, float z) => new(new Vector3(x, y, z), Vector3.UnitZ, Vector2.Zero);

    [Fact]
    public void Vertex_is_32_bytes_so_attribute_offsets_hold()
    {
        Assert.Equal(32, System.Runtime.InteropServices.Marshal.SizeOf<Vertex3D>());
    }

    [Fact]
    public void Bounds_cover_every_vertex()
    {
        var mesh = new MeshData([At(-1f, 0f, 2f), At(3f, 5f, 0f), At(0f, -2f, 1f)], [0, 1, 2]);

        Assert.Equal(new Vector3(-1f, -2f, 0f), mesh.BoundsMin);
        Assert.Equal(new Vector3(3f, 5f, 2f), mesh.BoundsMax);
        Assert.Equal(new Vector3(1f, 1.5f, 1f), mesh.BoundsCenter);
    }

    [Fact]
    public void Index_count_must_be_a_multiple_of_three()
    {
        Assert.Throws<ArgumentException>(() => new MeshData([At(0, 0, 0), At(1, 0, 0), At(0, 1, 0)], [0, 1]));
    }

    [Fact]
    public void Index_past_the_last_vertex_is_rejected_naming_the_index()
    {
        var ex = Assert.Throws<ArgumentException>(() => new MeshData([At(0, 0, 0), At(1, 0, 0), At(0, 1, 0)], [0, 1, 3]));

        Assert.Contains("Index 2 is 3", ex.Message);
    }

    [Fact]
    public void A_mesh_needs_vertices()
    {
        Assert.Throws<ArgumentException>(() => new MeshData([], []));
    }
}
