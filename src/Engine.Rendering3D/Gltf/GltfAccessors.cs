using System.Buffers.Binary;

namespace Engine.Rendering3D.Gltf;

/// <summary>
/// Reads accessors: typed, strided windows onto buffer bytes. Vertex attributes come out as packed <c>float</c>s (integers are
/// converted, normalised ones mapped to 0..1 or -1..1) and indices as <c>uint</c>s, so the rest of the importer never sees the
/// component type. Sparse accessors are rejected, not misread.
/// </summary>
public static class GltfAccessors
{
    private const int Byte = 5120;
    private const int UnsignedByte = 5121;
    private const int Short = 5122;
    private const int UnsignedShort = 5123;
    private const int UnsignedInt = 5125;
    private const int Float = 5126;

    /// <summary>Number of components for an accessor type: SCALAR 1, VEC2 2, VEC3 3, VEC4 4, and MAT4 16 (inverse bind matrices; other matrix sizes are not used).</summary>
    /// <exception cref="InvalidOperationException">The type is not one of those.</exception>
    public static int ComponentCount(string type) => type switch
    {
        "SCALAR" => 1,
        "VEC2" => 2,
        "VEC3" => 3,
        "VEC4" => 4,
        "MAT4" => 16,
        _ => throw new InvalidOperationException($"glTF accessor type '{type}' is not supported (use SCALAR, VEC2, VEC3, VEC4 or MAT4)."),
    };

    /// <summary>Reads an accessor as floats, <c>Count * components</c> of them, in element order.</summary>
    /// <param name="file">The parsed document.</param>
    /// <param name="buffers">The loaded buffers (<see cref="GltfBuffers.Load"/>).</param>
    /// <param name="index">Accessor index.</param>
    /// <param name="expectedComponents">The component count the caller needs (3 for positions); a mismatch is an error.</param>
    /// <exception cref="InvalidOperationException">The accessor is out of range, sparse, of an unexpected shape, or reads past its buffer.</exception>
    public static float[] ReadFloats(GltfFile file, byte[][] buffers, int index, int expectedComponents)
    {
        var accessor = Validate(file, index, expectedComponents);
        int components = expectedComponents;
        var result = new float[accessor.Count * components];
        if (accessor.BufferView == null) return result; // no view: the spec says all zeros

        var reader = Window(file, buffers, accessor, index, components, out int stride, out int componentSize);
        for (int element = 0; element < accessor.Count; element++)
        {
            for (int c = 0; c < components; c++)
            {
                var bytes = reader.Slice(element * stride + c * componentSize, componentSize);
                result[element * components + c] = ReadAsFloat(bytes, accessor.ComponentType, accessor.Normalized, index);
            }
        }

        return result;
    }

    /// <summary>Reads an index accessor (unsigned byte, short or int, SCALAR) as <c>uint</c>s.</summary>
    /// <exception cref="InvalidOperationException">The accessor is out of range, sparse, not a scalar of an unsigned integer type, or reads past its buffer.</exception>
    public static uint[] ReadIndices(GltfFile file, byte[][] buffers, int index)
    {
        var accessor = Validate(file, index, 1);
        if (accessor.ComponentType is not (UnsignedByte or UnsignedShort or UnsignedInt))
            throw new InvalidOperationException($"glTF accessor {index}: indices must be unsigned byte, short or int, got component type {accessor.ComponentType}.");

        var result = new uint[accessor.Count];
        if (accessor.BufferView == null) return result;

        var reader = Window(file, buffers, accessor, index, 1, out int stride, out int componentSize);
        for (int element = 0; element < accessor.Count; element++)
        {
            var bytes = reader.Slice(element * stride, componentSize);
            result[element] = accessor.ComponentType switch
            {
                UnsignedByte => bytes[0],
                UnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(bytes),
                _ => BinaryPrimitives.ReadUInt32LittleEndian(bytes),
            };
        }

        return result;
    }

    private static GltfFile.AccessorInfo Validate(GltfFile file, int index, int expectedComponents)
    {
        if (index < 0 || index >= file.Accessors.Count)
            throw new InvalidOperationException($"glTF accessor {index} does not exist (the file has {file.Accessors.Count}).");
        var accessor = file.Accessors[index];
        if (accessor.Sparse != null) throw new InvalidOperationException($"glTF accessor {index} is sparse, which is not supported.");
        if (accessor.Count < 0) throw new InvalidOperationException($"glTF accessor {index} has a negative count.");
        int actual = ComponentCount(accessor.Type);
        if (actual != expectedComponents)
            throw new InvalidOperationException($"glTF accessor {index} is {accessor.Type} but {expectedComponents} components were expected.");
        return accessor;
    }

    /// <summary>The bytes the accessor may touch, starting at its first element, with the element stride and component size.</summary>
    private static ReadOnlySpan<byte> Window(GltfFile file, byte[][] buffers, GltfFile.AccessorInfo accessor, int index, int components, out int stride, out int componentSize)
    {
        int viewIndex = accessor.BufferView!.Value;
        if (viewIndex < 0 || viewIndex >= file.BufferViews.Count)
            throw new InvalidOperationException($"glTF accessor {index} uses buffer view {viewIndex}, which does not exist.");
        var view = file.BufferViews[viewIndex];
        if (view.Buffer < 0 || view.Buffer >= buffers.Length)
            throw new InvalidOperationException($"glTF buffer view {viewIndex} uses buffer {view.Buffer}, which does not exist.");

        componentSize = accessor.ComponentType switch
        {
            Byte or UnsignedByte => 1,
            Short or UnsignedShort => 2,
            UnsignedInt or Float => 4,
            _ => throw new InvalidOperationException($"glTF accessor {index} has unknown component type {accessor.ComponentType}."),
        };
        int elementSize = components * componentSize;
        stride = view.ByteStride is > 0 ? view.ByteStride.Value : elementSize;
        if (stride < elementSize) throw new InvalidOperationException($"glTF buffer view {viewIndex} has a byteStride of {stride}, smaller than the {elementSize}-byte element.");

        long start = (long)view.ByteOffset + accessor.ByteOffset;
        long needed = accessor.Count == 0 ? 0 : (long)(accessor.Count - 1) * stride + elementSize;
        var buffer = buffers[view.Buffer];
        if (start < 0 || start + needed > buffer.Length || accessor.ByteOffset + needed > view.ByteLength)
            throw new InvalidOperationException($"glTF accessor {index} reads past the end of its buffer view or buffer.");
        return buffer.AsSpan((int)start, (int)needed);
    }

    private static float ReadAsFloat(ReadOnlySpan<byte> bytes, int componentType, bool normalized, int accessorIndex) => componentType switch
    {
        Float => BinaryPrimitives.ReadSingleLittleEndian(bytes),
        UnsignedByte => normalized ? bytes[0] / 255f : bytes[0],
        Byte => normalized ? MathF.Max((sbyte)bytes[0] / 127f, -1f) : (sbyte)bytes[0],
        UnsignedShort => normalized ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) / 65535f : BinaryPrimitives.ReadUInt16LittleEndian(bytes),
        Short => normalized ? MathF.Max(BinaryPrimitives.ReadInt16LittleEndian(bytes) / 32767f, -1f) : BinaryPrimitives.ReadInt16LittleEndian(bytes),
        _ => throw new InvalidOperationException($"glTF accessor {accessorIndex}: component type {componentType} cannot be read as a vertex attribute."),
    };
}
