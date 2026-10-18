using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Makes bright areas glow. Three steps: keep only pixels brighter than <see cref="Threshold"/>, blur that at half resolution
/// (a separable Gaussian: horizontal then vertical is far cheaper than one 2D blur), then add the blur back onto the image.
/// </summary>
public sealed class BloomEffect : IPostEffect
{
    private const string BrightFragment = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform float uThreshold;
        out vec4 FragColor;

        void main()
        {
            vec3 c = texture(uTexture, vUv).rgb;
            float peak = max(c.r, max(c.g, c.b));
            float keep = max(peak - uThreshold, 0.0) / max(peak, 0.0001);
            FragColor = vec4(c * keep, 1.0);
        }
        """;

    private const string BlurFragment = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform vec2 uStep;                                       // one texel along the blur direction
        out vec4 FragColor;

        void main()
        {
            const float w[5] = float[](0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);
            vec3 sum = texture(uTexture, vUv).rgb * w[0];
            for (int i = 1; i < 5; i++)
            {
                sum += texture(uTexture, vUv + uStep * float(i)).rgb * w[i];
                sum += texture(uTexture, vUv - uStep * float(i)).rgb * w[i];
            }
            FragColor = vec4(sum, 1.0);
        }
        """;

    private const string CompositeFragment = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform sampler2D uBloom;
        uniform float uIntensity;
        out vec4 FragColor;

        void main()
        {
            vec3 c = texture(uTexture, vUv).rgb + texture(uBloom, vUv).rgb * uIntensity;
            FragColor = vec4(min(c, vec3(1.0)), 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly Shader _bright;
    private readonly Shader _blur;
    private readonly Shader _composite;
    private RenderTarget? _ping;
    private RenderTarget? _pong;

    /// <summary>Compiles the three shaders. Needs a current GL context.</summary>
    public BloomEffect(GL gl)
    {
        _gl = gl;
        _bright = new Shader(gl, FullscreenQuad.VertexSource, BrightFragment);
        _blur = new Shader(gl, FullscreenQuad.VertexSource, BlurFragment);
        _composite = new Shader(gl, FullscreenQuad.VertexSource, CompositeFragment);
    }

    /// <inheritdoc />
    public string Name => "Bloom";

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <summary>Brightness (0..1) a pixel must exceed to glow. Lower means more of the image blooms.</summary>
    public float Threshold { get; set; } = 0.7f;

    /// <summary>How strongly the blurred glow is added back.</summary>
    public float Intensity { get; set; } = 0.8f;

    /// <summary>Number of horizontal+vertical blur rounds; more is a wider, softer glow.</summary>
    public int Blurs { get; set; } = 2;

    /// <inheritdoc />
    public void Apply(PostProcessContext context, Texture2D source, RenderTarget? destination)
    {
        int width = Math.Max(source.Width / 2, 1), height = Math.Max(source.Height / 2, 1);
        _ping = Ensure(_ping, width, height);
        _pong = Ensure(_pong, width, height);

        // 1. Bright pass: full-resolution source -> half-resolution ping.
        context.BindDestination(_ping);
        _bright.Use();
        source.Bind(0);
        _bright.SetUniform("uTexture", 0);
        _bright.SetUniform("uThreshold", Threshold);
        context.Quad.Draw();

        // 2. Blur: ping -> pong (horizontal), pong -> ping (vertical), repeated.
        _blur.Use();
        _blur.SetUniform("uTexture", 0);
        for (int i = 0; i < Math.Clamp(Blurs, 1, 8); i++)
        {
            Blur(context, _ping, _pong, new System.Numerics.Vector2(1f / width, 0f));
            Blur(context, _pong, _ping, new System.Numerics.Vector2(0f, 1f / height));
        }

        // 3. Composite: source + blurred glow -> destination.
        context.BindDestination(destination);
        _composite.Use();
        source.Bind(0);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _ping.Texture.Handle);
        _composite.SetUniform("uTexture", 0);
        _composite.SetUniform("uBloom", 1);
        _composite.SetUniform("uIntensity", Intensity);
        context.Quad.Draw();
        _gl.ActiveTexture(TextureUnit.Texture0);
    }

    /// <summary>Releases the shaders and the half-resolution targets.</summary>
    public void Dispose()
    {
        _bright.Dispose();
        _blur.Dispose();
        _composite.Dispose();
        _ping?.Dispose();
        _pong?.Dispose();
    }

    private RenderTarget Ensure(RenderTarget? target, int width, int height)
    {
        if (target == null) return new RenderTarget(_gl, width, height);

        target.Resize(width, height);
        return target;
    }

    private void Blur(PostProcessContext context, RenderTarget from, RenderTarget to, System.Numerics.Vector2 step)
    {
        context.BindDestination(to);
        from.Texture.Bind(0);
        _blur.SetUniform("uStep", step);
        context.Quad.Draw();
    }
}
