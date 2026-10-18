using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering3D;

/// <summary>
/// The skinning data of one vertex: which four joints move it and how much each counts (32 bytes, uploaded as a second vertex stream).
/// Joint numbers are stored as floats so the shader can read them as a plain <c>vec4</c> and index its joint array with them.
/// </summary>
/// <param name="Joints">Four indices into the skin's joint list.</param>
/// <param name="Weights">Four weights that add up to 1 (unused slots have weight 0).</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct SkinVertex(Vector4 Joints, Vector4 Weights);
