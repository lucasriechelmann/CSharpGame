using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

/// <summary>
/// A <see cref="MeshData"/> uploaded to the GPU: a vertex array object that remembers the vertex buffer, the index buffer and
/// how the 32-byte <see cref="Vertex3D"/> is split into attributes (0 = position, 1 = normal, 2 = uv). Uploaded once and never
/// rewritten, so the buffers are static. Owns its GL objects, so it is an asset the <c>AssetManager</c> disposes.
/// </summary>
public sealed class Mesh : IDisposable
{
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;

    /// <summary>Uploads the data. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the buffers.</param>
    /// <param name="data">The vertices and indices; also kept for the bounds.</param>
    public unsafe Mesh(GL gl, MeshData data)
    {
        _gl = gl;
        Data = data;

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        _vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData<Vertex3D>(BufferTargetARB.ArrayBuffer, data.Vertices, BufferUsageARB.StaticDraw);

        // The element buffer binding is part of the VAO state, so it is bound while the VAO is.
        _ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, data.Indices, BufferUsageARB.StaticDraw);

        uint stride = (uint)sizeof(Vertex3D);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, (void*)(6 * sizeof(float)));

        gl.BindVertexArray(0);
    }

    /// <summary>The CPU copy of the geometry (bounds, picking, saving).</summary>
    public MeshData Data { get; }

    /// <summary>Centre of the bounding sphere in local space.</summary>
    public Vector3 BoundsCenter => Data.BoundsCenter;

    /// <summary>Radius of the bounding sphere in local space.</summary>
    public float BoundsRadius => Data.BoundsRadius;

    /// <summary>Number of indices drawn per call.</summary>
    public int IndexCount => Data.Indices.Length;

    /// <summary>Draws every triangle with the shader and uniforms the caller has already set.</summary>
    public unsafe void Draw()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)IndexCount, DrawElementsType.UnsignedInt, (void*)0);
    }

    /// <summary>Deletes the vertex array and both buffers.</summary>
    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
    }
}
