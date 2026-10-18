using System.Buffers.Binary;
using System.Text;

namespace Engine.Rendering3D.Gltf;

/// <summary>
/// Opens glTF files: a text <c>.gltf</c> is just JSON, a binary <c>.glb</c> is a 12-byte header (magic "glTF", version 2, total
/// length) followed by chunks, each (length, type, data): the first is the JSON, an optional second is the binary buffer.
/// </summary>
public static class GltfContainer
{
    private const uint Magic = 0x46546C67;      // "glTF" read as a little-endian uint
    private const uint JsonChunk = 0x4E4F534A;  // "JSON"
    private const uint BinaryChunk = 0x004E4942; // "BIN\0"

    /// <summary>Reads a .gltf or .glb file, telling them apart by content, not by extension.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidOperationException">A .glb is truncated, has the wrong version or has no JSON chunk.</exception>
    public static GltfContents Read(string path) => Read(File.ReadAllBytes(path), path);

    /// <summary>Reads a glTF asset from memory.</summary>
    /// <param name="bytes">The whole file.</param>
    /// <param name="name">What to call the file in error messages.</param>
    /// <exception cref="InvalidOperationException">A .glb is truncated, has the wrong version or has no JSON chunk.</exception>
    public static GltfContents Read(byte[] bytes, string name = "glTF data")
    {
        if (bytes.Length < 4 || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic)
            return new GltfContents(Encoding.UTF8.GetString(bytes).TrimStart('﻿'), null);

        if (bytes.Length < 12) throw new InvalidOperationException($"{name}: the .glb header is truncated.");
        uint version = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        if (version != 2) throw new InvalidOperationException($"{name}: only glb version 2 is supported, got {version}.");
        uint declared = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8));
        if (declared > bytes.Length) throw new InvalidOperationException($"{name}: the .glb says it is {declared} bytes but only {bytes.Length} are present.");

        string? json = null;
        byte[]? binary = null;
        int offset = 12;
        while (offset + 8 <= declared)
        {
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4));
            offset += 8;
            if (length > declared - offset) throw new InvalidOperationException($"{name}: a chunk of {length} bytes runs past the end of the file.");

            if (type == JsonChunk && json == null) json = Encoding.UTF8.GetString(bytes, offset, (int)length);
            else if (type == BinaryChunk && binary == null) binary = bytes.AsSpan(offset, (int)length).ToArray();
            // Chunks of other types must be ignored by readers, per the spec.
            offset += (int)((length + 3) & ~3u); // chunks are padded to 4 bytes
        }

        return json == null
            ? throw new InvalidOperationException($"{name}: the .glb has no JSON chunk.")
            : new GltfContents(json, binary);
    }
}
