using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>Computes vertex normals for geometry that arrived without them (a glTF primitive may omit NORMAL).</summary>
public static class NormalGenerator
{
    /// <summary>
    /// Smooth normals: each vertex gets the sum of the normals of the triangles that touch it, weighted by triangle area (the
    /// unnormalised cross product is already proportional to area), then normalised. Vertices no triangle uses keep up (+Y).
    /// </summary>
    /// <param name="positions">Vertex positions.</param>
    /// <param name="indices">Triangle list indices into <paramref name="positions"/>.</param>
    public static Vector3[] Smooth(IReadOnlyList<Vector3> positions, IReadOnlyList<uint> indices)
    {
        var normals = new Vector3[positions.Count];
        for (int i = 0; i + 2 < indices.Count; i += 3)
        {
            uint a = indices[i], b = indices[i + 1], c = indices[i + 2];
            var faceNormal = Vector3.Cross(positions[(int)b] - positions[(int)a], positions[(int)c] - positions[(int)a]);
            normals[a] += faceNormal;
            normals[b] += faceNormal;
            normals[c] += faceNormal;
        }

        for (int i = 0; i < normals.Length; i++)
            normals[i] = normals[i].LengthSquared() > 1e-20f ? Vector3.Normalize(normals[i]) : Vector3.UnitY;
        return normals;
    }
}
