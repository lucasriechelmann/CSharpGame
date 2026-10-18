using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Rendering3D.Gltf;

namespace Engine.Rendering3D.Tests;

public class GltfContainerTests
{
    private static byte[] SampleGlb() => GltfWriter.WriteGlb([new GltfExportPart("cube", MeshBuilder.Cube(Vector3.One), Color.Red, Matrix4x4.Identity)]);

    [Fact]
    public void Text_gltf_is_returned_as_json_without_a_binary_chunk()
    {
        var contents = GltfContainer.Read(Encoding.UTF8.GetBytes("{ \"asset\": { \"version\": \"2.0\" } }"));

        Assert.Contains("\"asset\"", contents.Json);
        Assert.Null(contents.BinaryChunk);
    }

    [Fact]
    public void Text_gltf_with_a_byte_order_mark_is_accepted()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("{}")).ToArray();

        Assert.Equal("{}", GltfContainer.Read(bytes).Json);
    }

    [Fact]
    public void Glb_yields_its_json_and_binary_chunks()
    {
        var contents = GltfContainer.Read(SampleGlb());

        Assert.StartsWith("{", contents.Json);
        Assert.NotNull(contents.BinaryChunk);
        Assert.True(contents.BinaryChunk!.Length > 0);
    }

    [Fact]
    public void Glb_with_the_wrong_version_is_rejected()
    {
        var glb = SampleGlb();
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(4), 1);

        var ex = Assert.Throws<InvalidOperationException>(() => GltfContainer.Read(glb));

        Assert.Contains("version 2", ex.Message);
    }

    [Fact]
    public void Truncated_glb_is_rejected()
    {
        var glb = SampleGlb();

        Assert.Throws<InvalidOperationException>(() => GltfContainer.Read(glb[..(glb.Length - 10)]));
        Assert.Throws<InvalidOperationException>(() => GltfContainer.Read(glb[..8]));
    }

    [Fact]
    public void Glb_whose_chunk_runs_past_the_end_is_rejected()
    {
        var glb = SampleGlb();
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(12), (uint)glb.Length); // first chunk claims to be as big as the file

        var ex = Assert.Throws<InvalidOperationException>(() => GltfContainer.Read(glb));

        Assert.Contains("runs past the end", ex.Message);
    }

    [Fact]
    public void Glb_without_a_json_chunk_is_rejected()
    {
        var glb = SampleGlb();
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(16), 0x12345678); // turn the JSON chunk into an unknown type

        Assert.Throws<InvalidOperationException>(() => GltfContainer.Read(glb));
    }
}
