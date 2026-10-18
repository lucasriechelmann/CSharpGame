using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Draws one triangle that covers the whole target, so a fragment shader runs once per pixel: the standard way to apply a
/// full-screen effect. The vertex shader makes the corners from <c>gl_VertexID</c>, so no vertex buffer is needed, only the empty
/// vertex array that OpenGL core profile insists on.
/// </summary>
public sealed class FullscreenQuad : IDisposable
{
    /// <summary>Vertex stage shared by every full-screen effect; provides <c>vUv</c> (0..1 across the target).</summary>
    public const string VertexSource = """
        #version 330 core
        out vec2 vUv;

        void main()
        {
            vec2 corner = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
            vUv = corner;
            gl_Position = vec4(corner * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly uint _vao;

    /// <summary>Creates the empty vertex array. Needs a current GL context.</summary>
    public FullscreenQuad(GL gl)
    {
        _gl = gl;
        _vao = gl.GenVertexArray();
    }

    /// <summary>Draws the covering triangle with whatever shader and textures are currently bound.</summary>
    public void Draw()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
    }

    /// <summary>Deletes the vertex array.</summary>
    public void Dispose() => _gl.DeleteVertexArray(_vao);
}
