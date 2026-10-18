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
    private readonly uint _skinVbo;

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
        gl.EnableVertexAttribArray(3);
        gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, (void*)(8 * sizeof(float)));

        // Skinned meshes carry a second stream with four joint indices and four weights per vertex (attributes 11 and 12).
        if (data.Skin != null)
        {
            _skinVbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _skinVbo);
            gl.BufferData<SkinVertex>(BufferTargetARB.ArrayBuffer, data.Skin, BufferUsageARB.StaticDraw);
            uint skinStride = (uint)sizeof(SkinVertex);
            gl.EnableVertexAttribArray(11);
            gl.VertexAttribPointer(11, 4, VertexAttribPointerType.Float, false, skinStride, (void*)0);
            gl.EnableVertexAttribArray(12);
            gl.VertexAttribPointer(12, 4, VertexAttribPointerType.Float, false, skinStride, (void*)(4 * sizeof(float)));
        }

        gl.BindVertexArray(0);
    }

    /// <summary>True when the mesh has joint indices and weights and must be drawn with a skeleton's joint matrices.</summary>
    public bool IsSkinned => Data.Skin != null;

    /// <summary>The CPU copy of the geometry (bounds, picking, saving).</summary>
    public MeshData Data { get; }

    /// <summary>Centre of the bounding sphere in local space.</summary>
    public Vector3 BoundsCenter => Data.BoundsCenter;

    /// <summary>Radius of the bounding sphere in local space.</summary>
    public float BoundsRadius => Data.BoundsRadius;

    /// <summary>Number of indices drawn per call.</summary>
    public int IndexCount => Data.Indices.Length;

    /// <summary>Makes this mesh's vertex array current, so per-instance attributes can be attached to it before an instanced draw.</summary>
    public void BindVertexArray() => _gl.BindVertexArray(_vao);

    /// <summary>
    /// Draws the mesh <paramref name="count"/> times in one call. The caller has already bound this mesh's vertex array
    /// (<see cref="BindVertexArray"/>), attached the per-instance attributes and set the shader's uniforms.
    /// </summary>
    /// <param name="count">How many instances to draw.</param>
    public unsafe void DrawInstanced(int count) =>
        _gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)IndexCount, DrawElementsType.UnsignedInt, (void*)0, (uint)count);

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
        if (_skinVbo != 0) _gl.DeleteBuffer(_skinVbo);
        _gl.DeleteVertexArray(_vao);
    }
}
