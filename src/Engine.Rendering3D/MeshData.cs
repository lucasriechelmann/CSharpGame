using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// A mesh as plain CPU data: vertices plus a triangle list of indices, and the bounds computed from them. Keeping the data apart
/// from the GPU object (<see cref="Mesh"/>) means builders, loaders and culling can be unit-tested without a window.
/// </summary>
public sealed class MeshData
{
    /// <summary>Wraps vertices and triangle indices, validating them and measuring the bounds.</summary>
    /// <param name="vertices">At least one vertex.</param>
    /// <param name="indices">Three indices per triangle, counter-clockwise when seen from the front (the side the normal points to).</param>
    /// <exception cref="ArgumentException">No vertices, the index count is not a multiple of 3, or an index points past the last vertex.</exception>
    public MeshData(Vertex3D[] vertices, uint[] indices)
    {
        if (vertices.Length == 0) throw new ArgumentException("A mesh needs at least one vertex.", nameof(vertices));
        if (indices.Length % 3 != 0) throw new ArgumentException($"Index count {indices.Length} is not a multiple of 3 (triangle list).", nameof(indices));
        for (int i = 0; i < indices.Length; i++)
            if (indices[i] >= vertices.Length)
                throw new ArgumentException($"Index {i} is {indices[i]} but the mesh has only {vertices.Length} vertices.", nameof(indices));

        Vertices = vertices;
        Indices = indices;

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        BoundsMin = min;
        BoundsMax = max;
        BoundsCenter = (min + max) * 0.5f;
        BoundsRadius = (max - min).Length() * 0.5f;
    }

    /// <summary>The vertices.</summary>
    public Vertex3D[] Vertices { get; }

    /// <summary>The triangle list: three vertex indices per triangle.</summary>
    public uint[] Indices { get; }

    /// <summary>Smallest corner of the axis-aligned box around the vertices (local space).</summary>
    public Vector3 BoundsMin { get; }

    /// <summary>Largest corner of the axis-aligned box around the vertices (local space).</summary>
    public Vector3 BoundsMax { get; }

    /// <summary>Centre of the bounding box.</summary>
    public Vector3 BoundsCenter { get; }

    /// <summary>Radius of the sphere around <see cref="BoundsCenter"/> that contains the box; used for frustum culling.</summary>
    public float BoundsRadius { get; }

    /// <summary>Number of triangles.</summary>
    public int TriangleCount => Indices.Length / 3;
}
