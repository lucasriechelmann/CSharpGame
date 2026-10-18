using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering3D;

/// <summary>
/// One mesh vertex as the GPU reads it: position, surface normal, texture coordinate and tangent (48 bytes, tightly packed, so the
/// array can be uploaded as-is and the vertex attribute offsets are 0, 12, 24 and 32).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D
{
    /// <summary>Position in the mesh's local space.</summary>
    public Vector3 Position;

    /// <summary>Unit-length surface normal in local space; lighting needs it.</summary>
    public Vector3 Normal;

    /// <summary>Texture coordinate; (0,0) is the top-left of the image, like <c>Texture2D</c>.</summary>
    public Vector2 Uv;

    /// <summary>
    /// Direction of increasing U along the surface (xyz, unit length, perpendicular to the normal) and the handedness of the tangent frame
    /// (w, +1 or -1): the bitangent is <c>cross(normal, tangent.xyz) * tangent.w</c>, as in glTF. Normal maps need it. Left at zero, it is
    /// worked out from the UVs when the <see cref="MeshData"/> is built (<see cref="TangentGenerator"/>).
    /// </summary>
    public Vector4 Tangent;

    /// <summary>Creates a vertex without a tangent (computed later from the UVs).</summary>
    public Vertex3D(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Normal = normal;
        Uv = uv;
        Tangent = default;
    }

    /// <summary>Creates a vertex with an explicit tangent.</summary>
    public Vertex3D(Vector3 position, Vector3 normal, Vector2 uv, Vector4 tangent)
    {
        Position = position;
        Normal = normal;
        Uv = uv;
        Tangent = tangent;
    }
}
