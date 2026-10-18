using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// Works out per-vertex tangents from the texture coordinates, which a normal map needs: it stores directions in a frame that follows the
/// surface (tangent along U, bitangent along "up the image", normal out), and the shader must rebuild that frame at every pixel.
/// </summary>
public static class TangentGenerator
{
    /// <summary>
    /// Fills in <see cref="Vertex3D.Tangent"/> for every vertex. Each triangle contributes the direction in which U grows (and the
    /// matching bitangent) to its three vertices; the sums are made perpendicular to the vertex normal (Gram-Schmidt) and normalised, and
    /// the handedness records whether the UV mapping is mirrored. Vertices with a degenerate UV mapping get any tangent perpendicular to
    /// the normal, so a normal map there is harmless rather than NaN.
    /// </summary>
    /// <param name="vertices">The vertices; their normals and UVs are read, their tangents are replaced.</param>
    /// <param name="indices">The triangle list.</param>
    public static void Generate(Vertex3D[] vertices, uint[] indices)
    {
        var tangents = new Vector3[vertices.Length];
        var bitangents = new Vector3[vertices.Length];

        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            uint a = indices[i], b = indices[i + 1], c = indices[i + 2];
            var edge1 = vertices[b].Position - vertices[a].Position;
            var edge2 = vertices[c].Position - vertices[a].Position;
            var uv1 = vertices[b].Uv - vertices[a].Uv;
            var uv2 = vertices[c].Uv - vertices[a].Uv;

            // V grows down the image, but a normal map's green channel points up it (the glTF convention), so the "up" direction is -V.
            float du1 = uv1.X, dv1 = -uv1.Y, du2 = uv2.X, dv2 = -uv2.Y;
            float determinant = du1 * dv2 - du2 * dv1;
            if (MathF.Abs(determinant) < 1e-12f) continue; // a triangle with no UV area says nothing about direction

            float inverse = 1f / determinant;
            var tangent = (edge1 * dv2 - edge2 * dv1) * inverse;
            var bitangent = (edge2 * du1 - edge1 * du2) * inverse;
            tangents[a] += tangent; tangents[b] += tangent; tangents[c] += tangent;
            bitangents[a] += bitangent; bitangents[b] += bitangent; bitangents[c] += bitangent;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            var normal = vertices[i].Normal;
            var tangent = tangents[i] - normal * Vector3.Dot(normal, tangents[i]);
            if (tangent.LengthSquared() < 1e-16f) tangent = AnyPerpendicular(normal);
            tangent = Vector3.Normalize(tangent);

            float handedness = Vector3.Dot(Vector3.Cross(normal, tangent), bitangents[i]) < 0f ? -1f : 1f;
            vertices[i].Tangent = new Vector4(tangent, handedness);
        }
    }

    /// <summary>True when at least one vertex already has a tangent (they are all zero when the builder did not supply any).</summary>
    /// <param name="vertices">The vertices.</param>
    public static bool HasTangents(Vertex3D[] vertices)
    {
        foreach (var vertex in vertices)
        {
            if (vertex.Tangent != default) return true;
        }

        return false;
    }

    private static Vector3 AnyPerpendicular(Vector3 normal)
    {
        var other = MathF.Abs(normal.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
        return Vector3.Cross(normal, other);
    }
}
