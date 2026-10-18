using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Tilemaps;

/// <summary>The GPU side of a cached chunk: the quads of its static tiles in one vertex buffer and one index buffer, drawn with a single call. Owns its GL objects.</summary>
internal sealed class TileChunkMesh : IDisposable
{
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _indexCount;

    /// <summary>Uploads the quads (four vertices each, laid out as <see cref="SpriteQuad"/> writes them). Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the buffers.</param>
    /// <param name="vertices">The vertices of every quad.</param>
    public unsafe TileChunkMesh(GL gl, IReadOnlyList<SpriteVertex> vertices)
    {
        _gl = gl;
        QuadCount = vertices.Count / SpriteQuad.VertexCount;
        _indexCount = (uint)(QuadCount * SpriteQuad.IndexCount);

        // Each quad uses the same pattern the sprite batch does: 0,1,2 and 2,3,0 of its four vertices. A chunk has at most 256 tiles, so 16-bit indices are plenty.
        var indices = new ushort[QuadCount * SpriteQuad.IndexCount];
        for (int quad = 0; quad < QuadCount; quad++)
        {
            ushort v = (ushort)(quad * SpriteQuad.VertexCount);
            int i = quad * SpriteQuad.IndexCount;
            indices[i + 0] = v;
            indices[i + 1] = (ushort)(v + 1);
            indices[i + 2] = (ushort)(v + 2);
            indices[i + 3] = (ushort)(v + 2);
            indices[i + 4] = (ushort)(v + 3);
            indices[i + 5] = v;
        }

        var array = new SpriteVertex[vertices.Count];
        for (int i = 0; i < array.Length; i++) array[i] = vertices[i];

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);
        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData<SpriteVertex>(BufferTargetARB.ArrayBuffer, array, BufferUsageARB.StaticDraw);
        _ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<ushort>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

        uint stride = (uint)sizeof(SpriteVertex);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));
        gl.BindVertexArray(0);
    }

    /// <summary>How many tiles the mesh holds.</summary>
    public int QuadCount { get; }

    /// <summary>Draws every quad. The shader and texture must already be set.</summary>
    public unsafe void Draw()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedShort, (void*)0);
    }

    /// <summary>Frees the buffers.</summary>
    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
    }
}
