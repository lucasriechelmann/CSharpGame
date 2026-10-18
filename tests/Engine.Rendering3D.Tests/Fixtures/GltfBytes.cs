using System.Buffers.Binary;
using System.Numerics;
using Engine.Rendering3D.Gltf;

namespace Engine.Rendering3D.Tests.Fixtures;

/// <summary>Builds tiny glTF documents in memory so each test can state exactly the bytes and structure it cares about.</summary>
internal static class GltfBytes
{
    public static byte[] Floats(params float[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++) BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), values[i]);
        return bytes;
    }

    public static byte[] UShorts(params ushort[] values)
    {
        var bytes = new byte[values.Length * 2];
        for (int i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), values[i]);
        return bytes;
    }

    public static string DataUri(byte[] bytes) => "data:application/octet-stream;base64," + Convert.ToBase64String(bytes);

    /// <summary>A file with one buffer and a view over all of it; add accessors and meshes to the result.</summary>
    public static GltfFile FileWithBuffer(byte[] bytes, int? stride = null) => new()
    {
        Asset = new GltfFile.AssetInfo { Version = "2.0" },
        Buffers = [new GltfFile.BufferInfo { ByteLength = bytes.Length }],
        BufferViews = [new GltfFile.BufferViewInfo { Buffer = 0, ByteLength = bytes.Length, ByteStride = stride }],
    };

    public static GltfFile.AccessorInfo Accessor(int componentType, int count, string type, int byteOffset = 0, bool normalized = false) => new()
    {
        BufferView = 0,
        ComponentType = componentType,
        Count = count,
        Type = type,
        ByteOffset = byteOffset,
        Normalized = normalized,
    };

    /// <summary>
    /// A complete .gltf JSON text: one triangle (positions only unless <paramref name="withNormals"/>), a node transform
    /// (<paramref name="nodeJson"/>, e.g. <c>"translation":[1,2,3]</c>) and an optional material.
    /// </summary>
    public static string TriangleJson(string nodeJson = "", bool withNormals = true, bool indexed = true, string materialsJson = "", string primitiveExtra = "")
    {
        var data = new List<byte>();
        data.AddRange(Floats(0, 0, 0, 1, 0, 0, 0, 1, 0));            // positions: bytes 0..35
        data.AddRange(Floats(0, 0, 1, 0, 0, 1, 0, 0, 1));            // normals: 36..71
        data.AddRange(UShorts(0, 1, 2, 0));                          // indices (padded to 4): 72..79
        string buffer = DataUri([.. data]);
        string attributes = withNormals ? "\"POSITION\":0,\"NORMAL\":1" : "\"POSITION\":0";
        string indices = indexed ? ",\"indices\":2" : "";
        string material = materialsJson.Length > 0 ? ",\"material\":0" : "";
        string nodeBody = nodeJson.Length > 0 ? "," + nodeJson : "";
        return $$"""
            {
              "asset": { "version": "2.0" },
              "scene": 0,
              "scenes": [ { "nodes": [0] } ],
              "nodes": [ { "mesh": 0 {{nodeBody}} } ],
              "meshes": [ { "primitives": [ { "attributes": { {{attributes}} } {{indices}} {{material}} {{primitiveExtra}} } ] } ],
              {{materialsJson}}
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5126, "count": 3, "type": "VEC3" },
                { "bufferView": 2, "componentType": 5123, "count": 3, "type": "SCALAR" }
              ],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": 36 },
                { "buffer": 0, "byteOffset": 36, "byteLength": 36 },
                { "buffer": 0, "byteOffset": 72, "byteLength": 6 }
              ],
              "buffers": [ { "byteLength": 80, "uri": "{{buffer}}" } ]
            }
            """;
    }

    public static ModelData Import(string json) => GltfImporter.Import(new GltfContents(json, null), null);

    public static Vector3 Transform(ModelData data, Vector3 localPoint) => Vector3.Transform(localPoint, data.Parts[0].Local);
}
