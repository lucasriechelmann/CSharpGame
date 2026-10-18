using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Base for effects that are one fragment shader over the whole image. A subclass supplies the fragment source (reading
/// <c>uTexture</c> at <c>vUv</c>) and uploads its settings in <see cref="SetUniforms"/>; this class does the pass plumbing.
/// </summary>
public abstract class SinglePassEffect : IPostEffect
{
    private readonly Shader _shader;

    /// <summary>Compiles the effect's shader. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the shader.</param>
    /// <param name="name">Name shown in tools.</param>
    /// <param name="fragmentSource">GLSL 330 fragment stage with <c>in vec2 vUv</c>, <c>uniform sampler2D uTexture</c> and <c>out vec4 FragColor</c>.</param>
    protected SinglePassEffect(GL gl, string name, string fragmentSource)
    {
        Name = name;
        _shader = new Shader(gl, FullscreenQuad.VertexSource, fragmentSource);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Apply(PostProcessContext context, Texture2D source, RenderTarget? destination)
    {
        context.BindDestination(destination);
        _shader.Use();
        source.Bind(0);
        _shader.SetUniform("uTexture", 0);
        SetUniforms(_shader);
        context.Quad.Draw();
    }

    /// <summary>Releases the shader.</summary>
    public void Dispose() => _shader.Dispose();

    /// <summary>Uploads the effect's current settings. The shader is already in use.</summary>
    protected abstract void SetUniforms(Shader shader);
}
