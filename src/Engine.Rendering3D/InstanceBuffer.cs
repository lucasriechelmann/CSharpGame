using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

/// <summary>
/// A growing GPU buffer of per-instance data for instanced mesh draws: each instance is a model matrix and a normal matrix (25 floats), read
/// by the mesh shader as vertex attributes that advance once per instance. One buffer is shared by every mesh; attaching it to a mesh's vertex
/// array just before a draw records which buffer the attributes read.
/// </summary>
internal sealed class InstanceBuffer : IDisposable
{
    /// <summary>Floats per instance: a 4x4 model matrix (16) and a 3x3 normal matrix (9).</summary>
    public const int FloatsPerInstance = 25;

    private const int Stride = FloatsPerInstance * sizeof(float);

    private readonly GL _gl;
    private readonly uint _buffer;
    private float[] _staging = new float[FloatsPerInstance * 64];
    private int _count;
    private int _capacityInstances;

    /// <summary>Creates the buffer. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that owns the buffer.</param>
    public InstanceBuffer(GL gl)
    {
        _gl = gl;
        _buffer = gl.GenBuffer();
    }

    /// <summary>Instances staged since the last <see cref="Clear"/>.</summary>
    public int Count => _count;

    /// <summary>Forgets the staged instances (the GPU memory stays allocated).</summary>
    public void Clear() => _count = 0;

    /// <summary>Stages one instance: its model matrix and the matrix that carries normals (the inverse transpose of the model matrix).</summary>
    /// <param name="model">Local-to-world matrix.</param>
    public void Add(in Matrix4x4 model)
    {
        int need = (_count + 1) * FloatsPerInstance;
        if (need > _staging.Length) Array.Resize(ref _staging, Math.Max(need, _staging.Length * 2));

        var normal = ModelMatrix.NormalMatrix(model);
        var at = _staging.AsSpan(_count * FloatsPerInstance, FloatsPerInstance);

        // Row by row: the matrices are row-vector, and GL reads each row as a column of the transposed matrix it expects.
        at[0] = model.M11; at[1] = model.M12; at[2] = model.M13; at[3] = model.M14;
        at[4] = model.M21; at[5] = model.M22; at[6] = model.M23; at[7] = model.M24;
        at[8] = model.M31; at[9] = model.M32; at[10] = model.M33; at[11] = model.M34;
        at[12] = model.M41; at[13] = model.M42; at[14] = model.M43; at[15] = model.M44;
        at[16] = normal.M11; at[17] = normal.M12; at[18] = normal.M13;
        at[19] = normal.M21; at[20] = normal.M22; at[21] = normal.M23;
        at[22] = normal.M31; at[23] = normal.M32; at[24] = normal.M33;
        _count++;
    }

    /// <summary>
    /// Uploads the staged instances and attaches them as vertex attributes 4 to 10 of the vertex array that is currently bound (the mesh's),
    /// advancing once per instance.
    /// </summary>
    public unsafe void UploadAndAttach()
    {
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _buffer);
        if (_count > _capacityInstances)
        {
            _capacityInstances = Math.Max(_count, _capacityInstances * 2);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_capacityInstances * Stride), null, BufferUsageARB.StreamDraw);
        }

        fixed (float* data = _staging) _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(_count * Stride), data);

        for (uint column = 0; column < 4; column++) Attribute(4 + column, 4, (int)column * 4);
        for (uint column = 0; column < 3; column++) Attribute(8 + column, 3, 16 + (int)column * 3);
    }

    /// <summary>Deletes the buffer.</summary>
    public void Dispose() => _gl.DeleteBuffer(_buffer);

    private unsafe void Attribute(uint location, int components, int floatOffset)
    {
        _gl.EnableVertexAttribArray(location);
        _gl.VertexAttribPointer(location, components, VertexAttribPointerType.Float, false, (uint)Stride, (void*)(floatOffset * sizeof(float)));
        _gl.VertexAttribDivisor(location, 1);
    }
}
