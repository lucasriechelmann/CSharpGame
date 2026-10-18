using Engine.Rendering3D.Gltf;
using Engine.Rendering3D.Tests.Fixtures;

namespace Engine.Rendering3D.Tests;

public class GltfAccessorTests
{
    [Fact]
    public void Tightly_packed_vec3_floats_are_read_in_order()
    {
        var bytes = GltfBytes.Floats(1, 2, 3, 4, 5, 6);
        var file = GltfBytes.FileWithBuffer(bytes);
        file.Accessors = [GltfBytes.Accessor(5126, 2, "VEC3")];

        Assert.Equal([1f, 2f, 3f, 4f, 5f, 6f], GltfAccessors.ReadFloats(file, [bytes], 0, 3));
    }

    [Fact]
    public void Interleaved_data_honours_the_byte_stride_and_offset()
    {
        // Two vertices of (x, y, z, u, v) in one 20-byte-stride view; positions start at 0, uvs at byte offset 12.
        var bytes = GltfBytes.Floats(1, 2, 3, 0.25f, 0.5f, 4, 5, 6, 0.75f, 1f);
        var file = GltfBytes.FileWithBuffer(bytes, stride: 20);
        file.Accessors = [GltfBytes.Accessor(5126, 2, "VEC3"), GltfBytes.Accessor(5126, 2, "VEC2", byteOffset: 12)];

        Assert.Equal([1f, 2f, 3f, 4f, 5f, 6f], GltfAccessors.ReadFloats(file, [bytes], 0, 3));
        Assert.Equal([0.25f, 0.5f, 0.75f, 1f], GltfAccessors.ReadFloats(file, [bytes], 1, 2));
    }

    [Fact]
    public void Normalised_unsigned_bytes_map_to_zero_one()
    {
        byte[] bytes = [0, 255, 51, 102];
        var file = GltfBytes.FileWithBuffer(bytes);
        file.Accessors = [GltfBytes.Accessor(5121, 2, "VEC2", normalized: true)];

        var uv = GltfAccessors.ReadFloats(file, [bytes], 0, 2);

        Assert.Equal([0f, 1f, 0.2f, 0.4f], uv);
    }

    [Fact]
    public void Normalised_unsigned_shorts_map_to_zero_one()
    {
        var bytes = GltfBytes.UShorts(0, 65535);
        var file = GltfBytes.FileWithBuffer(bytes);
        file.Accessors = [GltfBytes.Accessor(5123, 1, "VEC2", normalized: true)];

        Assert.Equal([0f, 1f], GltfAccessors.ReadFloats(file, [bytes], 0, 2));
    }

    [Fact]
    public void Indices_of_each_unsigned_type_are_widened()
    {
        var shorts = GltfBytes.UShorts(0, 1, 70);
        var fileShorts = GltfBytes.FileWithBuffer(shorts);
        fileShorts.Accessors = [GltfBytes.Accessor(5123, 3, "SCALAR")];
        byte[] bytes = [3, 4, 255];
        var fileBytes = GltfBytes.FileWithBuffer(bytes);
        fileBytes.Accessors = [GltfBytes.Accessor(5121, 3, "SCALAR")];
        var ints = new byte[8];
        BitConverter.TryWriteBytes(ints, 100000u);
        BitConverter.TryWriteBytes(ints.AsSpan(4), 7u);
        var fileInts = GltfBytes.FileWithBuffer(ints);
        fileInts.Accessors = [GltfBytes.Accessor(5125, 2, "SCALAR")];

        Assert.Equal([0u, 1u, 70u], GltfAccessors.ReadIndices(fileShorts, [shorts], 0));
        Assert.Equal([3u, 4u, 255u], GltfAccessors.ReadIndices(fileBytes, [bytes], 0));
        Assert.Equal([100000u, 7u], GltfAccessors.ReadIndices(fileInts, [ints], 0));
    }

    [Fact]
    public void Float_indices_are_rejected()
    {
        var bytes = GltfBytes.Floats(1, 2, 3);
        var file = GltfBytes.FileWithBuffer(bytes);
        file.Accessors = [GltfBytes.Accessor(5126, 3, "SCALAR")];

        Assert.Throws<InvalidOperationException>(() => GltfAccessors.ReadIndices(file, [bytes], 0));
    }

    [Fact]
    public void Reading_past_the_end_of_the_buffer_is_an_error_naming_the_accessor()
    {
        var bytes = GltfBytes.Floats(1, 2, 3);
        var file = GltfBytes.FileWithBuffer(bytes);
        file.Accessors = [GltfBytes.Accessor(5126, 2, "VEC3")]; // needs 24 bytes, only 12 exist

        var ex = Assert.Throws<InvalidOperationException>(() => GltfAccessors.ReadFloats(file, [bytes], 0, 3));

        Assert.Contains("accessor 0", ex.Message);
    }

    [Fact]
    public void Component_count_mismatch_is_rejected()
    {
        var bytes = GltfBytes.Floats(1, 2, 3);
        var file = GltfBytes.FileWithBuffer(bytes);
        file.Accessors = [GltfBytes.Accessor(5126, 1, "VEC3")];

        Assert.Throws<InvalidOperationException>(() => GltfAccessors.ReadFloats(file, [bytes], 0, 2));
    }

    [Fact]
    public void Sparse_accessors_are_rejected_instead_of_misread()
    {
        var bytes = GltfBytes.Floats(1, 2, 3);
        var file = GltfBytes.FileWithBuffer(bytes);
        var accessor = GltfBytes.Accessor(5126, 1, "VEC3");
        accessor.Sparse = System.Text.Json.JsonDocument.Parse("{}").RootElement;
        file.Accessors = [accessor];

        var ex = Assert.Throws<InvalidOperationException>(() => GltfAccessors.ReadFloats(file, [bytes], 0, 3));

        Assert.Contains("sparse", ex.Message);
    }

    [Fact]
    public void Accessor_without_a_buffer_view_reads_as_zeros()
    {
        var file = GltfBytes.FileWithBuffer([]);
        file.Accessors = [new GltfFile.AccessorInfo { ComponentType = 5126, Count = 2, Type = "VEC2" }];

        Assert.Equal([0f, 0f, 0f, 0f], GltfAccessors.ReadFloats(file, [[]], 0, 2));
    }

    [Fact]
    public void Unknown_accessor_index_is_rejected()
    {
        var file = GltfBytes.FileWithBuffer([]);

        Assert.Throws<InvalidOperationException>(() => GltfAccessors.ReadFloats(file, [[]], 3, 3));
    }

    [Fact]
    public void Stride_smaller_than_the_element_is_rejected()
    {
        var bytes = GltfBytes.Floats(1, 2, 3, 4, 5, 6);
        var file = GltfBytes.FileWithBuffer(bytes, stride: 8);
        file.Accessors = [GltfBytes.Accessor(5126, 2, "VEC3")];

        Assert.Throws<InvalidOperationException>(() => GltfAccessors.ReadFloats(file, [bytes], 0, 3));
    }
}
