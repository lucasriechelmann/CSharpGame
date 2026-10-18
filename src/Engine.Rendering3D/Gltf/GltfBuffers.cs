namespace Engine.Rendering3D.Gltf;

/// <summary>Resolves the byte data a glTF file refers to: embedded <c>data:</c> URIs, files next to the glTF, and the .glb binary chunk.</summary>
public static class GltfBuffers
{
    /// <summary>Loads every buffer of the file.</summary>
    /// <param name="file">The parsed document.</param>
    /// <param name="binaryChunk">The .glb binary chunk, used by the buffer that has no uri (null for a text .gltf).</param>
    /// <param name="baseDirectory">Folder that relative uris resolve against; null when the asset is not on disk (then only data URIs and the chunk work).</param>
    /// <exception cref="InvalidOperationException">A buffer cannot be found or is shorter than it declares.</exception>
    public static byte[][] Load(GltfFile file, byte[]? binaryChunk, string? baseDirectory)
    {
        var buffers = new byte[file.Buffers.Count][];
        for (int i = 0; i < buffers.Length; i++)
        {
            var info = file.Buffers[i];
            byte[] data;
            if (info.Uri == null)
            {
                data = binaryChunk ?? throw new InvalidOperationException($"glTF buffer {i} has no uri and the file has no binary (.glb) chunk.");
            }
            else
            {
                data = ReadUri(info.Uri, baseDirectory, $"buffer {i}");
            }

            if (data.Length < info.ByteLength)
                throw new InvalidOperationException($"glTF buffer {i} declares {info.ByteLength} bytes but only {data.Length} are available.");
            buffers[i] = data;
        }

        return buffers;
    }

    /// <summary>The bytes a uri names: decoded base64 for a <c>data:</c> URI, the file content for a relative path.</summary>
    /// <param name="uri">The uri from the glTF JSON (percent-encoded paths are decoded).</param>
    /// <param name="baseDirectory">Folder for relative paths; null when unavailable.</param>
    /// <param name="what">What the uri belongs to, for error messages ("buffer 0", "image 2").</param>
    public static byte[] ReadUri(string uri, string? baseDirectory, string what)
    {
        if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            int comma = uri.IndexOf(',');
            if (comma < 0 || !uri.AsSpan(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"glTF {what}: only base64 data URIs are supported.");
            try
            {
                return Convert.FromBase64String(uri[(comma + 1)..]);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"glTF {what}: the base64 data is not valid.", ex);
            }
        }

        if (baseDirectory == null)
            throw new InvalidOperationException($"glTF {what} refers to the file '{uri}' but the asset was not loaded from disk.");

        string path = Path.GetFullPath(Path.Combine(baseDirectory, Uri.UnescapeDataString(uri)));
        if (!File.Exists(path)) throw new FileNotFoundException($"glTF {what} refers to '{uri}', which does not exist.", path);
        return File.ReadAllBytes(path);
    }
}
