using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering;

/// <summary>One corner of a sprite quad as uploaded to the GPU. The layout must match the vertex attributes set up in <see cref="SpriteBatch"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpriteVertex
{
    /// <summary>World-space position (Z is draw order).</summary>
    public Vector3 Position;

    /// <summary>Texture coordinate, (0,0) = top-left of the texture.</summary>
    public Vector2 TexCoord;

    /// <summary>Tint multiplied with the sampled texture colour.</summary>
    public Color Color;
}
