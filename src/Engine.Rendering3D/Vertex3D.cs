using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering3D;

/// <summary>
/// One mesh vertex as the GPU reads it: position, surface normal and texture coordinate (32 bytes, tightly packed, so the
/// array can be uploaded as-is and the vertex attribute offsets are 0, 12 and 24).
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

    /// <summary>Creates a vertex.</summary>
    public Vertex3D(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Normal = normal;
        Uv = uv;
    }
}
