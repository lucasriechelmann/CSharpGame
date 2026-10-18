using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Rendering3D.Gltf;

/// <summary>
/// The subset of the glTF 2.0 JSON schema the importer reads: scenes, nodes, meshes, accessors, buffers, materials, textures and images.
/// Everything else (animations, skins, cameras, samplers, extensions) is ignored, except <c>extensionsRequired</c>, which makes the
/// import fail rather than silently draw something wrong. Property names follow the spec (camelCase).
/// </summary>
public sealed class GltfFile
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>Required header; <see cref="AssetInfo.Version"/> must be 2.x.</summary>
    public AssetInfo? Asset { get; set; }

    /// <summary>Index of the scene to import; the first scene when absent.</summary>
    public int? Scene { get; set; }

    /// <summary>The scenes: each lists its root nodes.</summary>
    public List<SceneInfo> Scenes { get; set; } = [];

    /// <summary>All nodes (a tree through <see cref="NodeInfo.Children"/>).</summary>
    public List<NodeInfo> Nodes { get; set; } = [];

    /// <summary>All meshes; each is a list of primitives.</summary>
    public List<MeshInfo> Meshes { get; set; } = [];

    /// <summary>Typed views onto buffer bytes.</summary>
    public List<AccessorInfo> Accessors { get; set; } = [];

    /// <summary>Byte ranges of buffers.</summary>
    public List<BufferViewInfo> BufferViews { get; set; } = [];

    /// <summary>The raw byte blobs (data URI, external file, or the binary chunk of a .glb).</summary>
    public List<BufferInfo> Buffers { get; set; } = [];

    /// <summary>Surface descriptions.</summary>
    public List<MaterialInfo> Materials { get; set; } = [];

    /// <summary>Textures: an image plus a sampler (the sampler is ignored).</summary>
    public List<TextureInfo> Textures { get; set; } = [];

    /// <summary>Encoded images (PNG/JPEG), by uri or inside a buffer view.</summary>
    public List<ImageInfo> Images { get; set; } = [];

    /// <summary>Extensions the file cannot be displayed correctly without.</summary>
    public List<string> ExtensionsRequired { get; set; } = [];

    /// <summary>Parses glTF JSON.</summary>
    /// <exception cref="InvalidOperationException">The JSON is invalid, not a glTF 2.x asset, or lists a required extension.</exception>
    public static GltfFile Parse(string json)
    {
        GltfFile file;
        try
        {
            file = JsonSerializer.Deserialize<GltfFile>(json, Options) ?? throw new InvalidOperationException("The glTF file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"glTF JSON is not valid: {ex.Message}", ex);
        }

        if (file.Asset?.Version is not { } version || !version.StartsWith("2.", StringComparison.Ordinal))
            throw new InvalidOperationException($"Only glTF 2.x is supported, the file says '{file.Asset?.Version ?? "(no asset.version)"}'.");
        if (file.ExtensionsRequired.Count > 0)
            throw new InvalidOperationException($"The glTF file requires extensions this engine does not support: {string.Join(", ", file.ExtensionsRequired)}.");
        return file;
    }

    /// <summary>The <c>asset</c> header.</summary>
    public sealed class AssetInfo
    {
        /// <summary>glTF version, "2.0".</summary>
        public string? Version { get; set; }
    }

    /// <summary>A scene: the root nodes to show.</summary>
    public sealed class SceneInfo
    {
        /// <summary>Indices of the root nodes.</summary>
        public List<int> Nodes { get; set; } = [];
    }

    /// <summary>A node: a transform, optionally a mesh, and children.</summary>
    public sealed class NodeInfo
    {
        /// <summary>Optional human-readable node name (animation channels and tools refer to nodes by it).</summary>
        public string? Name { get; set; }

        /// <summary>Index of the mesh drawn at this node.</summary>
        public int? Mesh { get; set; }

        /// <summary>Indices of child nodes.</summary>
        public List<int> Children { get; set; } = [];

        /// <summary>A 4x4 transform in column-major order (16 numbers); mutually exclusive with translation/rotation/scale.</summary>
        public float[]? Matrix { get; set; }

        /// <summary>Position (3 numbers).</summary>
        public float[]? Translation { get; set; }

        /// <summary>Rotation quaternion x, y, z, w (4 numbers).</summary>
        public float[]? Rotation { get; set; }

        /// <summary>Per-axis scale (3 numbers).</summary>
        public float[]? Scale { get; set; }
    }

    /// <summary>A mesh: one or more primitives (each draws with one material).</summary>
    public sealed class MeshInfo
    {
        /// <summary>The primitives.</summary>
        public List<PrimitiveInfo> Primitives { get; set; } = [];
    }

    /// <summary>One draw: vertex attribute accessors, an optional index accessor and a material.</summary>
    public sealed class PrimitiveInfo
    {
        /// <summary>Attribute name (POSITION, NORMAL, TEXCOORD_0, ...) to accessor index.</summary>
        public Dictionary<string, int> Attributes { get; set; } = [];

        /// <summary>Accessor of the indices; absent means the vertices are already a triangle list.</summary>
        public int? Indices { get; set; }

        /// <summary>Index into <see cref="GltfFile.Materials"/>; absent means the default material.</summary>
        public int? Material { get; set; }

        /// <summary>Topology; 4 (triangles) is the default and the only supported mode.</summary>
        public int Mode { get; set; } = 4;
    }

    /// <summary>A typed view (component type, element type, count) onto a buffer view.</summary>
    public sealed class AccessorInfo
    {
        /// <summary>The buffer view the data lives in; absent means all zeros.</summary>
        public int? BufferView { get; set; }

        /// <summary>Offset inside the buffer view in bytes.</summary>
        public int ByteOffset { get; set; }

        /// <summary>5120 byte, 5121 unsigned byte, 5122 short, 5123 unsigned short, 5125 unsigned int, 5126 float.</summary>
        public int ComponentType { get; set; }

        /// <summary>Whether integer data maps to 0..1 (or -1..1) floats.</summary>
        public bool Normalized { get; set; }

        /// <summary>Number of elements.</summary>
        public int Count { get; set; }

        /// <summary>SCALAR, VEC2, VEC3, VEC4, MAT2, MAT3 or MAT4.</summary>
        public string Type { get; set; } = "";

        /// <summary>Present when the accessor is sparse, which is not supported.</summary>
        public JsonElement? Sparse { get; set; }
    }

    /// <summary>A byte range of a buffer, optionally interleaved (stride).</summary>
    public sealed class BufferViewInfo
    {
        /// <summary>The buffer.</summary>
        public int Buffer { get; set; }

        /// <summary>Start of the range in bytes.</summary>
        public int ByteOffset { get; set; }

        /// <summary>Length of the range in bytes.</summary>
        public int ByteLength { get; set; }

        /// <summary>Distance between consecutive elements; absent means tightly packed.</summary>
        public int? ByteStride { get; set; }
    }

    /// <summary>A raw byte blob.</summary>
    public sealed class BufferInfo
    {
        /// <summary>A <c>data:</c> URI or a file path relative to the glTF file; absent for the .glb binary chunk.</summary>
        public string? Uri { get; set; }

        /// <summary>Size in bytes.</summary>
        public int ByteLength { get; set; }
    }

    /// <summary>A surface description (only the metallic-roughness colour is used).</summary>
    public sealed class MaterialInfo
    {
        /// <summary>The PBR parameters.</summary>
        public PbrInfo? PbrMetallicRoughness { get; set; }

        /// <summary>Draw back faces too.</summary>
        public bool DoubleSided { get; set; }
    }

    /// <summary>Metallic-roughness parameters.</summary>
    public sealed class PbrInfo
    {
        /// <summary>RGBA multiplier, 0..1; white when absent.</summary>
        public float[]? BaseColorFactor { get; set; }

        /// <summary>The colour texture.</summary>
        public TextureRef? BaseColorTexture { get; set; }

        /// <summary>0 = mirror-like metal... 1 = fully rough (default 1).</summary>
        public float RoughnessFactor { get; set; } = 1f;

        /// <summary>0 = dielectric, 1 = metal (default 1).</summary>
        public float MetallicFactor { get; set; } = 1f;
    }

    /// <summary>A reference to a texture.</summary>
    public sealed class TextureRef
    {
        /// <summary>Index into <see cref="GltfFile.Textures"/>.</summary>
        public int Index { get; set; }
    }

    /// <summary>A texture: which image it shows.</summary>
    public sealed class TextureInfo
    {
        /// <summary>Index into <see cref="GltfFile.Images"/>.</summary>
        public int? Source { get; set; }
    }

    /// <summary>An encoded image.</summary>
    public sealed class ImageInfo
    {
        /// <summary>A <c>data:</c> URI or a file path relative to the glTF file.</summary>
        public string? Uri { get; set; }

        /// <summary>The buffer view holding the encoded bytes when there is no uri.</summary>
        public int? BufferView { get; set; }
    }
}
