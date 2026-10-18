using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>Darkens the image towards its corners, which pulls the eye to the centre where the hero is.</summary>
public sealed class VignetteEffect(GL gl) : SinglePassEffect(gl, "Vignette", FragmentSource)
{
    private const string FragmentSource = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform float uIntensity;
        uniform float uRadius;
        uniform float uSoftness;
        out vec4 FragColor;

        void main()
        {
            vec3 c = texture(uTexture, vUv).rgb;
            float d = distance(vUv, vec2(0.5)) * 1.4142;          // 0 at the centre, about 1 at the corners
            float shade = smoothstep(uRadius, uRadius + uSoftness, d);
            FragColor = vec4(c * (1.0 - shade * uIntensity), 1.0);
        }
        """;

    /// <summary>How dark the corners get: 0 none, 1 black.</summary>
    public float Intensity { get; set; } = 0.45f;

    /// <summary>Distance from the centre (0 centre, 1 corner) where darkening starts.</summary>
    public float Radius { get; set; } = 0.55f;

    /// <summary>How far past <see cref="Radius"/> the darkening takes to reach full strength; larger is gentler.</summary>
    public float Softness { get; set; } = 0.6f;

    /// <inheritdoc />
    protected override void SetUniforms(Shader shader)
    {
        shader.SetUniform("uIntensity", Intensity);
        shader.SetUniform("uRadius", Radius);
        shader.SetUniform("uSoftness", Math.Max(Softness, 0.001f));
    }
}
