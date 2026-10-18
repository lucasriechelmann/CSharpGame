using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Draws plain triangles from world-space points, with no colour of their own: the light map uses it to stamp each light's shadow
/// shapes into the stencil buffer (the caller turns colour writes off and sets the stencil state). Owns one vertex buffer that is
/// refilled for every call.
/// </summary>
internal sealed class ShadowVolumeRenderer : IDisposable
{
    private const string VertexSource = """
        #version 330 core
        layout (location = 0) in vec2 aPosition;
        uniform mat4 uViewProjection;

        void main()
        {
            gl_Position = uViewProjection * vec4(aPosition, 0.0, 1.0);
        }
        """;

    private const string FragmentSource = """
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(0.0);
        }
        """;

    private readonly GL _gl;
    private readonly Shader _shader;
    private readonly uint _vao;
    private readonly uint _vbo;

    /// <summary>Creates the renderer. Needs a current GL context.</summary>
    public unsafe ShadowVolumeRenderer(GL gl)
    {
        _gl = gl;
        _shader = new Shader(gl, VertexSource, FragmentSource);
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vector2), (void*)0);
        gl.BindVertexArray(0);
    }

    /// <summary>Draws the points as a triangle list.</summary>
    /// <param name="triangles">Three points per triangle, in world space.</param>
    /// <param name="viewProjection">The camera's view-projection matrix.</param>
    public void Draw(List<Vector2> triangles, in Matrix4x4 viewProjection)
    {
        if (triangles.Count == 0) return;

        _shader.Use();
        _shader.SetUniform("uViewProjection", viewProjection);
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, CollectionsMarshal.AsSpan(triangles), BufferUsageARB.DynamicDraw);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)triangles.Count);
        _gl.BindVertexArray(0);
    }

    /// <summary>Releases the buffer, array and shader.</summary>
    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _shader.Dispose();
    }
}
