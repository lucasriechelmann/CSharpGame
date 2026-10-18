using System.Buffers.Binary;
using System.IO.Compression;

namespace Engine.Editor.Projects;

/// <summary>A tiny PNG encoder (8-bit RGBA, no filtering) so the editor can create picture files, such as a new project's sample sprite, without an imaging library.</summary>
public static class PngWriter
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Encodes an image.</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="rgba">Four bytes per pixel, rows top to bottom.</param>
    /// <exception cref="ArgumentException">The pixel count does not match the size.</exception>
    public static byte[] Encode(int width, int height, ReadOnlySpan<byte> rgba)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "An image needs a positive size.");
        if (rgba.Length != width * height * 4) throw new ArgumentException($"Expected {width * height * 4} bytes for {width}x{height} RGBA, got {rgba.Length}.", nameof(rgba));

        // Every scanline starts with a filter byte; 0 means "none".
        var raw = new byte[height * (1 + width * 4)];
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (1 + width * 4);
            raw[rowStart] = 0;
            rgba.Slice(y * width * 4, width * 4).CopyTo(raw.AsSpan(rowStart + 1));
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) zlib.Write(raw);

        using var png = new MemoryStream();
        png.Write(Signature);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;  // bit depth
        header[9] = 6;  // colour type: RGBA
        header[10] = 0; // compression
        header[11] = 0; // filter method
        header[12] = 0; // no interlacing
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        var typeAndData = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) typeAndData[i] = (byte)type[i];
        data.CopyTo(typeAndData.AsSpan(4));
        stream.Write(typeAndData);

        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeAndData));
        stream.Write(crc);
    }

    /// <summary>The CRC-32 every PNG chunk ends with (the same polynomial zip uses).</summary>
    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }

        return ~crc;
    }
}
