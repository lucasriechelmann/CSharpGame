using System.Numerics;
using Engine.Rendering;

namespace Engine.Rendering3D.Gltf;

/// <summary>
/// Turns a glTF asset into <see cref="ModelData"/>: walks the scene's node tree, multiplying transforms down it, and converts every
/// triangle primitive into a part with engine-format vertices. glTF and this engine agree on the conventions that matter (right-handed,
/// Y up, counter-clockwise front faces, texture row 0 at the top), so geometry needs no flipping. Skins, animations, cameras, lights
/// and sparse data are not imported; unsupported things that would draw wrongly fail loudly instead.
/// </summary>
public static class GltfImporter
{
    private const int MaxDepth = 64;

    /// <summary>Reads a .gltf or .glb file from disk (external buffers and images resolve next to it).</summary>
    /// <exception cref="InvalidOperationException">The file is not valid or uses something unsupported; the message says what and where.</exception>
    public static ModelData ImportFile(string path)
    {
        var contents = GltfContainer.Read(path);
        return Import(contents, Path.GetDirectoryName(Path.GetFullPath(path)));
    }

    /// <summary>Imports an opened asset.</summary>
    /// <param name="contents">The JSON and, for a .glb, the binary chunk.</param>
    /// <param name="baseDirectory">Folder for relative uris; null when the asset only uses data URIs and the binary chunk.</param>
    /// <exception cref="InvalidOperationException">The asset is not valid or uses something unsupported.</exception>
    public static ModelData Import(GltfContents contents, string? baseDirectory)
    {
        var file = GltfFile.Parse(contents.Json);
        var buffers = GltfBuffers.Load(file, contents.BinaryChunk, baseDirectory);

        var materials = new List<ModelMaterialData>();
        foreach (var material in file.Materials) materials.Add(ImportMaterial(file, buffers, baseDirectory, material));
        int defaultMaterial = -1; // added lazily: only when some primitive has no material

        var parts = new List<ModelPartData>();
        foreach (int root in RootNodes(file))
            Visit(file, buffers, root, Matrix4x4.Identity, 0, parts, materials, ref defaultMaterial);

        return new ModelData(parts, materials);
    }

    private static IEnumerable<int> RootNodes(GltfFile file)
    {
        if (file.Scenes.Count > 0)
        {
            int scene = file.Scene ?? 0;
            if (scene < 0 || scene >= file.Scenes.Count)
                throw new InvalidOperationException($"glTF scene {scene} does not exist (the file has {file.Scenes.Count}).");
            return file.Scenes[scene].Nodes;
        }

        // No scenes: show every node nothing else owns, which is what a viewer would do with a scene-less file.
        var children = new HashSet<int>(file.Nodes.SelectMany(n => n.Children));
        return Enumerable.Range(0, file.Nodes.Count).Where(i => !children.Contains(i));
    }

    private static void Visit(GltfFile file, byte[][] buffers, int nodeIndex, Matrix4x4 parent, int depth, List<ModelPartData> parts, List<ModelMaterialData> materials, ref int defaultMaterial)
    {
        if (nodeIndex < 0 || nodeIndex >= file.Nodes.Count)
            throw new InvalidOperationException($"glTF node {nodeIndex} does not exist (the file has {file.Nodes.Count}).");
        if (depth > MaxDepth) throw new InvalidOperationException($"glTF node tree is deeper than {MaxDepth} levels (is it a cycle?).");

        var node = file.Nodes[nodeIndex];
        // Row-vector convention: the child's own matrix applies first, then its parent's.
        var world = LocalMatrix(node, nodeIndex) * parent;

        if (node.Mesh is int meshIndex)
        {
            if (meshIndex < 0 || meshIndex >= file.Meshes.Count)
                throw new InvalidOperationException($"glTF node {nodeIndex} uses mesh {meshIndex}, which does not exist.");
            foreach (var primitive in file.Meshes[meshIndex].Primitives)
            {
                int material = primitive.Material ?? -1;
                if (primitive.Material is int requested && (requested < 0 || requested >= file.Materials.Count))
                    throw new InvalidOperationException($"glTF mesh {meshIndex} uses material {requested}, which does not exist (the file has {file.Materials.Count}).");
                if (material < 0)
                {
                    if (defaultMaterial < 0)
                    {
                        defaultMaterial = materials.Count;
                        materials.Add(ModelMaterialData.Default);
                    }

                    material = defaultMaterial;
                }

                parts.Add(new ModelPartData(ImportPrimitive(file, buffers, primitive, meshIndex), material, world));
            }
        }

        foreach (int child in node.Children)
            Visit(file, buffers, child, world, depth + 1, parts, materials, ref defaultMaterial);
    }

    private static Matrix4x4 LocalMatrix(GltfFile.NodeInfo node, int index)
    {
        if (node.Matrix != null)
        {
            if (node.Matrix.Length != 16) throw new InvalidOperationException($"glTF node {index}: 'matrix' needs 16 numbers, got {node.Matrix.Length}.");
            // glTF stores the matrix column-major for column vectors; read straight into System.Numerics' row-major fields it is the
            // row-vector matrix we use (a transpose of a transpose).
            var m = node.Matrix;
            return new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        }

        var translation = Vec3(node.Translation, Vector3.Zero, index, "translation");
        var scale = Vec3(node.Scale, Vector3.One, index, "scale");
        var rotation = Quaternion.Identity;
        if (node.Rotation != null)
        {
            if (node.Rotation.Length != 4) throw new InvalidOperationException($"glTF node {index}: 'rotation' needs 4 numbers, got {node.Rotation.Length}.");
            rotation = Quaternion.Normalize(new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]));
        }

        return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
    }

    private static Vector3 Vec3(float[]? values, Vector3 fallback, int node, string name)
    {
        if (values == null) return fallback;
        if (values.Length != 3) throw new InvalidOperationException($"glTF node {node}: '{name}' needs 3 numbers, got {values.Length}.");
        return new Vector3(values[0], values[1], values[2]);
    }

    private static MeshData ImportPrimitive(GltfFile file, byte[][] buffers, GltfFile.PrimitiveInfo primitive, int meshIndex)
    {
        if (primitive.Mode != 4)
            throw new InvalidOperationException($"glTF mesh {meshIndex}: primitive mode {primitive.Mode} is not supported, only triangle lists (mode 4).");
        if (!primitive.Attributes.TryGetValue("POSITION", out int positionAccessor))
            throw new InvalidOperationException($"glTF mesh {meshIndex}: a primitive has no POSITION attribute.");

        var positions = GltfAccessors.ReadFloats(file, buffers, positionAccessor, 3);
        int vertexCount = positions.Length / 3;
        if (vertexCount == 0) throw new InvalidOperationException($"glTF mesh {meshIndex}: a primitive has no vertices.");

        uint[] indices;
        if (primitive.Indices is int indexAccessor)
        {
            indices = GltfAccessors.ReadIndices(file, buffers, indexAccessor);
        }
        else
        {
            indices = new uint[vertexCount]; // no indices: the vertices are already a triangle list
            for (uint i = 0; i < indices.Length; i++) indices[i] = i;
        }

        var positionVectors = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++) positionVectors[i] = new Vector3(positions[i * 3], positions[i * 3 + 1], positions[i * 3 + 2]);

        float[]? normals = primitive.Attributes.TryGetValue("NORMAL", out int normalAccessor) ? GltfAccessors.ReadFloats(file, buffers, normalAccessor, 3) : null;
        float[]? uvs = primitive.Attributes.TryGetValue("TEXCOORD_0", out int uvAccessor) ? GltfAccessors.ReadFloats(file, buffers, uvAccessor, 2) : null;
        if (normals != null && normals.Length != positions.Length) throw new InvalidOperationException($"glTF mesh {meshIndex}: NORMAL has {normals.Length / 3} entries but POSITION has {vertexCount}.");
        if (uvs != null && uvs.Length / 2 != vertexCount) throw new InvalidOperationException($"glTF mesh {meshIndex}: TEXCOORD_0 has {uvs.Length / 2} entries but POSITION has {vertexCount}.");

        // Without NORMAL the spec says to use flat shading; smooth normals are the pragmatic choice for the usually shared vertices.
        var generated = normals == null ? NormalGenerator.Smooth(positionVectors, indices) : null;

        var vertices = new Vertex3D[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            var normal = normals != null
                ? Normalised(new Vector3(normals[i * 3], normals[i * 3 + 1], normals[i * 3 + 2]))
                : generated![i];
            var uv = uvs != null ? new Vector2(uvs[i * 2], uvs[i * 2 + 1]) : Vector2.Zero;
            vertices[i] = new Vertex3D(positionVectors[i], normal, uv);
        }

        return new MeshData(vertices, indices);
    }

    private static Vector3 Normalised(Vector3 v) => v.LengthSquared() > 1e-20f ? Vector3.Normalize(v) : Vector3.UnitY;

    private static ModelMaterialData ImportMaterial(GltfFile file, byte[][] buffers, string? baseDirectory, GltfFile.MaterialInfo material)
    {
        var pbr = material.PbrMetallicRoughness;
        var color = Color.White;
        if (pbr?.BaseColorFactor is { } factor)
        {
            if (factor.Length != 4) throw new InvalidOperationException($"glTF material: baseColorFactor needs 4 numbers, got {factor.Length}.");
            color = new Color(factor[0], factor[1], factor[2], factor[3]);
        }

        byte[]? image = pbr?.BaseColorTexture is { } textureRef ? ReadImage(file, buffers, baseDirectory, textureRef.Index) : null;
        return ModelMaterialData.FromRoughness(color, image, pbr?.RoughnessFactor ?? 1f, pbr?.MetallicFactor ?? 1f, material.DoubleSided);
    }

    private static byte[] ReadImage(GltfFile file, byte[][] buffers, string? baseDirectory, int textureIndex)
    {
        if (textureIndex < 0 || textureIndex >= file.Textures.Count)
            throw new InvalidOperationException($"glTF texture {textureIndex} does not exist (the file has {file.Textures.Count}).");
        if (file.Textures[textureIndex].Source is not int imageIndex)
            throw new InvalidOperationException($"glTF texture {textureIndex} has no image source (only plain images are supported).");
        if (imageIndex < 0 || imageIndex >= file.Images.Count)
            throw new InvalidOperationException($"glTF image {imageIndex} does not exist (the file has {file.Images.Count}).");

        var image = file.Images[imageIndex];
        if (image.Uri != null) return GltfBuffers.ReadUri(image.Uri, baseDirectory, $"image {imageIndex}");

        if (image.BufferView is not int viewIndex || viewIndex < 0 || viewIndex >= file.BufferViews.Count)
            throw new InvalidOperationException($"glTF image {imageIndex} has neither a uri nor a valid bufferView.");
        var view = file.BufferViews[viewIndex];
        if (view.Buffer < 0 || view.Buffer >= buffers.Length || view.ByteOffset < 0 || view.ByteOffset + view.ByteLength > buffers[view.Buffer].Length)
            throw new InvalidOperationException($"glTF image {imageIndex}: its buffer view lies outside the buffer.");
        return buffers[view.Buffer].AsSpan(view.ByteOffset, view.ByteLength).ToArray();
    }
}
