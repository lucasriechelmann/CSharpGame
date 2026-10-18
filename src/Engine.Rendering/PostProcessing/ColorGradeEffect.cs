using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Adjusts the look of the whole image: brightness, contrast, saturation and a colour tint. The cheapest way to change a game's
/// mood (desaturate for danger, warm tint for dusk) without touching any art.
/// </summary>
public sealed class ColorGradeEffect(GL gl) : SinglePassEffect(gl, "Color grade", FragmentSource)
{
    private const string FragmentSource = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform float uBrightness;
        uniform float uContrast;
        uniform float uSaturation;
        uniform vec3 uTint;
        out vec4 FragColor;

        void main()
        {
            vec3 c = texture(uTexture, vUv).rgb;
            c *= uTint;
            c += uBrightness;
            c = (c - 0.5) * uContrast + 0.5;                      // contrast pivots around mid-grey
            float luma = dot(c, vec3(0.2126, 0.7152, 0.0722));
            c = mix(vec3(luma), c, uSaturation);                  // 0 = greyscale, 1 = unchanged
            FragColor = vec4(clamp(c, 0.0, 1.0), 1.0);
        }
        """;

    /// <summary>Added to every channel; 0 leaves the image unchanged, positive brightens.</summary>
    public float Brightness { get; set; }

    /// <summary>Scales the distance from mid-grey; 1 is unchanged, above 1 punchier, below 1 flatter.</summary>
    public float Contrast { get; set; } = 1f;

    /// <summary>0 is black and white, 1 unchanged, above 1 more vivid.</summary>
    public float Saturation { get; set; } = 1f;

    /// <summary>Multiplied into the colour; white (1, 1, 1) leaves it unchanged.</summary>
    public Vector3 Tint { get; set; } = Vector3.One;

    /// <inheritdoc />
    protected override void SetUniforms(Shader shader)
    {
        shader.SetUniform("uBrightness", Brightness);
        shader.SetUniform("uContrast", Contrast);
        shader.SetUniform("uSaturation", Saturation);
        shader.SetUniform("uTint", Tint);
    }
}
