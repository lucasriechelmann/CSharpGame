using System.Buffers.Binary;
using System.Numerics;
using Engine.Rendering;
using System.Text.Json;

namespace Engine.Rendering3D.Gltf;

/// <summary>
/// Writes geometry as a binary glTF (.glb): positions, normals, texture coordinates and 32-bit indices per piece, flat-coloured
/// materials and one node per piece. It exists so procedural shapes can be saved as a standard asset (the sample's model is made this
/// way), and gives the importer a round-trip test: what the writer produces, the importer must read back unchanged.
/// </summary>
public static class GltfWriter
{
    private const int ArrayBuffer = 34962;
    private const int ElementArrayBuffer = 34963;
    private const int FloatType = 5126;
    private const int UnsignedIntType = 5125;

    /// <summary>Builds the bytes of a .glb file holding the pieces.</summary>
    /// <exception cref="ArgumentException">No pieces were given.</exception>
    public static byte[] WriteGlb(IReadOnlyList<GltfExportPart> parts)
    {
        if (parts.Count == 0) throw new ArgumentException("A model needs at least one part.", nameof(parts));

        using var binary = new MemoryStream();
        var json = new MemoryStream();
        using (var w = new Utf8JsonWriter(json))
        {
            w.WriteStartObject();
            w.WriteStartObject("asset");
            w.WriteString("version", "2.0");
            w.WriteString("generator", "CSharpGame GltfWriter");
            w.WriteEndObject();
            w.WriteNumber("scene", 0);
            w.WriteStartArray("scenes");
            w.WriteStartObject();
            w.WriteStartArray("nodes");
            for (int i = 0; i < parts.Count; i++) w.WriteNumberValue(i);
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteEndArray();

            WriteNodes(w, parts);
            WriteMeshes(w, parts);
            var materials = DistinctColors(parts);
            WriteMaterials(w, materials);
            WriteAccessorsAndViews(w, parts, binary);

            w.WriteStartArray("buffers");
            w.WriteStartObject();
            w.WriteNumber("byteLength", (int)binary.Length);
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        }

        return Pack(json.ToArray(), binary.ToArray());
    }

    private static void WriteNodes(Utf8JsonWriter w, IReadOnlyList<GltfExportPart> parts)
    {
        w.WriteStartArray("nodes");
        for (int i = 0; i < parts.Count; i++)
        {
            var m = parts[i].Local;
            w.WriteStartObject();
            w.WriteString("name", parts[i].Name);
            w.WriteNumber("mesh", i);
            w.WriteStartArray("matrix"); // row-vector rows are glTF's column-major columns, so the fields go out in memory order
            foreach (float value in new[] { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 })
                w.WriteNumberValue(value);
            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteMeshes(Utf8JsonWriter w, IReadOnlyList<GltfExportPart> parts)
    {
        var materials = DistinctColors(parts);
        w.WriteStartArray("meshes");
        for (int i = 0; i < parts.Count; i++)
        {
            w.WriteStartObject();
            w.WriteStartArray("primitives");
            w.WriteStartObject();
            w.WriteStartObject("attributes");
            w.WriteNumber("POSITION", i * 4);
            w.WriteNumber("NORMAL", i * 4 + 1);
            w.WriteNumber("TEXCOORD_0", i * 4 + 2);
            w.WriteEndObject();
            w.WriteNumber("indices", i * 4 + 3);
            w.WriteNumber("material", materials.IndexOf(parts[i].Color));
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static List<Color> DistinctColors(IReadOnlyList<GltfExportPart> parts) => parts.Select(p => p.Color).Distinct().ToList();

    private static void WriteMaterials(Utf8JsonWriter w, List<Color> colors)
    {
        w.WriteStartArray("materials");
        foreach (var color in colors)
        {
            w.WriteStartObject();
            w.WriteStartObject("pbrMetallicRoughness");
            w.WriteStartArray("baseColorFactor");
            w.WriteNumberValue(color.R);
            w.WriteNumberValue(color.G);
            w.WriteNumberValue(color.B);
            w.WriteNumberValue(color.A);
            w.WriteEndArray();
            w.WriteNumber("metallicFactor", 0f);
            w.WriteNumber("roughnessFactor", 0.5f);
            w.WriteEndObject();
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteAccessorsAndViews(Utf8JsonWriter w, IReadOnlyList<GltfExportPart> parts, MemoryStream binary)
    {
        var views = new List<(int Offset, int Length, int Target)>();
        var accessors = new List<(int View, int Component, int Count, string Type, Vector3? Min, Vector3? Max)>();

        foreach (var part in parts)
        {
            var mesh = part.Mesh;
            int count = mesh.Vertices.Length;

            AddView(binary, views, count * 12, ArrayBuffer, span =>
            {
                for (int i = 0; i < count; i++) WriteVector3(span[(i * 12)..], mesh.Vertices[i].Position);
            });
            accessors.Add((views.Count - 1, FloatType, count, "VEC3", mesh.BoundsMin, mesh.BoundsMax));

            AddView(binary, views, count * 12, ArrayBuffer, span =>
            {
                for (int i = 0; i < count; i++) WriteVector3(span[(i * 12)..], mesh.Vertices[i].Normal);
            });
            accessors.Add((views.Count - 1, FloatType, count, "VEC3", null, null));

            AddView(binary, views, count * 8, ArrayBuffer, span =>
            {
                for (int i = 0; i < count; i++)
                {
                    BinaryPrimitives.WriteSingleLittleEndian(span[(i * 8)..], mesh.Vertices[i].Uv.X);
                    BinaryPrimitives.WriteSingleLittleEndian(span[(i * 8 + 4)..], mesh.Vertices[i].Uv.Y);
                }
            });
            accessors.Add((views.Count - 1, FloatType, count, "VEC2", null, null));

            AddView(binary, views, mesh.Indices.Length * 4, ElementArrayBuffer, span =>
            {
                for (int i = 0; i < mesh.Indices.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(span[(i * 4)..], mesh.Indices[i]);
            });
            accessors.Add((views.Count - 1, UnsignedIntType, mesh.Indices.Length, "SCALAR", null, null));
        }

        w.WriteStartArray("bufferViews");
        foreach (var (offset, length, target) in views)
        {
            w.WriteStartObject();
            w.WriteNumber("buffer", 0);
            w.WriteNumber("byteOffset", offset);
            w.WriteNumber("byteLength", length);
            w.WriteNumber("target", target);
            w.WriteEndObject();
        }

        w.WriteEndArray();

        w.WriteStartArray("accessors");
        foreach (var (view, component, count, type, min, max) in accessors)
        {
            w.WriteStartObject();
            w.WriteNumber("bufferView", view);
            w.WriteNumber("componentType", component);
            w.WriteNumber("count", count);
            w.WriteString("type", type);
            if (min is { } lo && max is { } hi)
            {
                w.WriteStartArray("min");
                w.WriteNumberValue(lo.X); w.WriteNumberValue(lo.Y); w.WriteNumberValue(lo.Z);
                w.WriteEndArray();
                w.WriteStartArray("max");
                w.WriteNumberValue(hi.X); w.WriteNumberValue(hi.Y); w.WriteNumberValue(hi.Z);
                w.WriteEndArray();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private delegate void Fill(Span<byte> bytes);

    private static void AddView(MemoryStream binary, List<(int Offset, int Length, int Target)> views, int length, int target, Fill fill)
    {
        var bytes = new byte[length];
        fill(bytes);
        views.Add(((int)binary.Position, length, target));
        binary.Write(bytes);
        while (binary.Position % 4 != 0) binary.WriteByte(0); // every view starts on a 4-byte boundary, as accessors require
    }

    private static void WriteVector3(Span<byte> destination, Vector3 v)
    {
        BinaryPrimitives.WriteSingleLittleEndian(destination, v.X);
        BinaryPrimitives.WriteSingleLittleEndian(destination[4..], v.Y);
        BinaryPrimitives.WriteSingleLittleEndian(destination[8..], v.Z);
    }

    /// <summary>Wraps the JSON and binary payloads in the .glb container: header, JSON chunk (space padded), BIN chunk (zero padded).</summary>
    private static byte[] Pack(byte[] json, byte[] binary)
    {
        int jsonPadded = (json.Length + 3) & ~3;
        int binaryPadded = (binary.Length + 3) & ~3;
        int total = 12 + 8 + jsonPadded + 8 + binaryPadded;
        var glb = new byte[total];

        BinaryPrimitives.WriteUInt32LittleEndian(glb, 0x46546C67);
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(8), (uint)total);

        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(12), (uint)jsonPadded);
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(16), 0x4E4F534A);
        json.CopyTo(glb, 20);
        for (int i = json.Length; i < jsonPadded; i++) glb[20 + i] = (byte)' ';

        int binaryChunk = 20 + jsonPadded;
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(binaryChunk), (uint)binaryPadded);
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(binaryChunk + 4), 0x004E4942);
        binary.CopyTo(glb, binaryChunk + 8);
        return glb;
    }
}
