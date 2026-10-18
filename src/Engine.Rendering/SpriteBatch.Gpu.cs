using Silk.NET.OpenGL;

namespace Engine.Rendering;

// OpenGL side of the batch: buffer creation, per-frame state and flushing.
public sealed partial class SpriteBatch
{
    private readonly GL _gl;
    private readonly Shader _shader;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;

    /// <summary>Creates the vertex array, buffers and built-in shader. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the GPU resources.</param>
    public unsafe SpriteBatch(GL gl)
    {
        _gl = gl;
        _shader = new Shader(gl, SpriteShaderSource.Vertex, SpriteShaderSource.Fragment);

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        // Vertex buffer: rewritten every flush, so DynamicDraw. Allocated once at full size.
        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(SpriteVertex)), null, BufferUsageARB.DynamicDraw);

        // Index buffer: every quad uses the same 0,1,2,2,3,0 pattern, so build it once. It is part of the VAO state.
        _ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<ushort>(BufferTargetARB.ElementArrayBuffer, BuildIndices(), BufferUsageARB.StaticDraw);

        uint stride = (uint)sizeof(SpriteVertex);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));

        gl.BindVertexArray(0);
    }

    /// <summary>Releases the vertex array, buffers and shader.</summary>
    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
        _shader.Dispose();
    }

    private static ushort[] BuildIndices()
    {
        var indices = new ushort[MaxSprites * SpriteQuad.IndexCount];
        for (int sprite = 0; sprite < MaxSprites; sprite++)
        {
            int i = sprite * SpriteQuad.IndexCount;
            ushort v = (ushort)(sprite * SpriteQuad.VertexCount);
            indices[i + 0] = v;
            indices[i + 1] = (ushort)(v + 1);
            indices[i + 2] = (ushort)(v + 2);
            indices[i + 3] = (ushort)(v + 2);
            indices[i + 4] = (ushort)(v + 3);
            indices[i + 5] = v;
        }

        return indices;
    }

    private void PrepareState()
    {
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(
            BlendingFactor.SrcAlpha,
            _blend == SpriteBlend.Additive ? BlendingFactor.One : BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);
    }

    private unsafe void Flush()
    {
        if (_spriteCount == 0) return;

        _shader.Use();
        _shader.SetUniform("uViewProjection", _viewProjection);
        _shader.SetUniform("uTexture", 0);
        _currentTexture!.Bind(0);

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferSubData<SpriteVertex>(BufferTargetARB.ArrayBuffer, 0, _vertices.AsSpan(0, _spriteCount * SpriteQuad.VertexCount));
        _gl.DrawElements(PrimitiveType.Triangles, (uint)(_spriteCount * SpriteQuad.IndexCount), DrawElementsType.UnsignedShort, (void*)0);

        _frameDrawCalls++;
        _spriteCount = 0;
    }
}
