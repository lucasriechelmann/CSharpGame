using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>A compiled and linked GLSL program (vertex + fragment). Owns the GL program object.</summary>
public sealed class Shader : IDisposable
{
    private readonly GL _gl;
    private readonly Dictionary<string, int> _uniformLocations = new();

    /// <summary>Compiles and links the two stages.</summary>
    /// <param name="gl">The GL context that will own the program.</param>
    /// <param name="vertexSource">GLSL source of the vertex stage.</param>
    /// <param name="fragmentSource">GLSL source of the fragment stage.</param>
    /// <exception cref="InvalidOperationException">A stage failed to compile or the program failed to link; the message holds the GL info log.</exception>
    public Shader(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;

        uint vertex = Compile(ShaderType.VertexShader, vertexSource);
        uint fragment = Compile(ShaderType.FragmentShader, fragmentSource);

        Handle = _gl.CreateProgram();
        _gl.AttachShader(Handle, vertex);
        _gl.AttachShader(Handle, fragment);
        _gl.LinkProgram(Handle);

        // The stages are copied into the program at link time, so the shader objects can go either way.
        _gl.DetachShader(Handle, vertex);
        _gl.DetachShader(Handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

        _gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out int linked);
        if (linked == 0)
        {
            string log = _gl.GetProgramInfoLog(Handle);
            _gl.DeleteProgram(Handle);
            throw new InvalidOperationException($"Shader program failed to link:\n{log}");
        }
    }

    /// <summary>The OpenGL program object name.</summary>
    public uint Handle { get; }

    /// <summary>Makes this program current for subsequent draw calls.</summary>
    public void Use() => _gl.UseProgram(Handle);

    /// <summary>Sets an <c>int</c> uniform (also used for sampler texture units). The program must be in use.</summary>
    public void SetUniform(string name, int value) => _gl.Uniform1(Location(name), value);

    /// <summary>Sets a <c>float</c> uniform. The program must be in use.</summary>
    public void SetUniform(string name, float value) => _gl.Uniform1(Location(name), value);

    /// <summary>Sets a <c>vec2</c> uniform. The program must be in use.</summary>
    public void SetUniform(string name, Vector2 value) => _gl.Uniform2(Location(name), value.X, value.Y);

    /// <summary>Sets a <c>vec3</c> uniform. The program must be in use.</summary>
    public void SetUniform(string name, Vector3 value) => _gl.Uniform3(Location(name), value.X, value.Y, value.Z);

    /// <summary>Sets a <c>vec4</c> uniform. The program must be in use.</summary>
    public void SetUniform(string name, Vector4 value) => _gl.Uniform4(Location(name), value.X, value.Y, value.Z, value.W);

    /// <summary>Sets a <c>mat4</c> uniform from a System.Numerics matrix without transposing (see <see cref="Camera2D.GetViewProjection"/>). The program must be in use.</summary>
    public void SetUniform(string name, in Matrix4x4 value)
    {
        ReadOnlySpan<float> floats = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Matrix4x4, float>(ref Unsafe.AsRef(in value)), 16);
        _gl.UniformMatrix4(Location(name), 1, false, floats);
    }

    /// <summary>
    /// Sets an array of <c>mat4</c> uniforms (a skeleton's joint matrices) in one call, again without transposing. <paramref name="name"/> is the
    /// array's first element, such as <c>uJoints[0]</c>. The program must be in use.
    /// </summary>
    /// <param name="name">Name of the array's first element.</param>
    /// <param name="values">The matrices; no more than the array holds.</param>
    public void SetUniform(string name, ReadOnlySpan<Matrix4x4> values)
    {
        if (values.IsEmpty) return;

        ReadOnlySpan<float> floats = MemoryMarshal.Cast<Matrix4x4, float>(values);
        _gl.UniformMatrix4(Location(name), (uint)values.Length, false, floats);
    }

    /// <summary>Deletes the GL program.</summary>
    public void Dispose() => _gl.DeleteProgram(Handle);

    private int Location(string name)
    {
        if (_uniformLocations.TryGetValue(name, out int location)) return location;

        location = _gl.GetUniformLocation(Handle, name);
        if (location < 0)
            throw new ArgumentException($"Shader has no active uniform named '{name}'.", nameof(name));
        _uniformLocations[name] = location;
        return location;
    }

    private uint Compile(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compiled);
        if (compiled == 0)
        {
            string log = _gl.GetShaderInfoLog(shader);
            _gl.DeleteShader(shader);
            throw new InvalidOperationException($"{type} failed to compile:\n{log}");
        }

        return shader;
    }
}
