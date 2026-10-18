using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Turns an HDR image (see <see cref="PostProcessChain.Hdr"/>) into something a screen can show. Bright lights can push values far
/// above 1; a plain 8-bit output would clip them to flat white, while a tone-map curve rolls them off smoothly. Put it last in the chain.
/// </summary>
public sealed class ToneMapEffect(GL gl) : SinglePassEffect(gl, "Tone map", FragmentSource)
{
    private const string FragmentSource = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform float uExposure;
        uniform int uMode;
        out vec4 FragColor;

        void main()
        {
            vec3 c = max(texture(uTexture, vUv).rgb * uExposure, vec3(0.0));
            if (uMode == 0)
            {
                c = c / (c + vec3(1.0));
            }
            else if (uMode == 1)
            {
                c = clamp((c * (2.51 * c + 0.03)) / (c * (2.43 * c + 0.59) + 0.14), 0.0, 1.0);
            }
            else
            {
                c = min(c, vec3(1.0));
            }

            FragColor = vec4(c, 1.0);
        }
        """;

    /// <summary>The curve to use.</summary>
    public ToneMapMode Mode { get; set; } = ToneMapMode.Aces;

    /// <summary>Multiplies the image before the curve: raises or lowers overall brightness.</summary>
    public float Exposure { get; set; } = 1f;

    /// <inheritdoc />
    protected override void SetUniforms(Shader shader)
    {
        shader.SetUniform("uExposure", Exposure);
        shader.SetUniform("uMode", (int)Mode);
    }
}
