using System.Numerics;
using Engine.Rendering;
using Silk.NET.OpenGL;
using Shader = Engine.Rendering.Shader;

namespace Engine.Rendering3D;

/// <summary>
/// Draws a procedural sky behind the 3D world: a gradient from the horizon colour up to the zenith colour, the ground colour below the horizon, and a
/// small sun disc with a glow in the direction of the scene's sun. One full-screen triangle, no textures, no cube map.
/// </summary>
/// <remarks>
/// Why a gradient instead of a cube map: it needs no image assets, it follows any camera orientation exactly, and it costs one cheap pass. The shader
/// rebuilds each pixel's world-space view direction from the inverse view-projection matrix, so the sky never moves when the camera moves, only when it turns.
/// </remarks>
public sealed class SkyRenderer : IDisposable
{
    private const string Fragment = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;

        uniform mat4 uInverseViewProjection;
        uniform vec3 uCameraPosition;
        uniform vec3 uZenith;
        uniform vec3 uHorizon;
        uniform vec3 uGround;
        uniform vec3 uToSun;
        uniform vec3 uSunColor;

        void main()
        {
            vec4 far = uInverseViewProjection * vec4(vUv * 2.0 - 1.0, 1.0, 1.0);
            vec3 dir = normalize(far.xyz / far.w - uCameraPosition);

            // Up: horizon to zenith, quickly at first (the square root keeps the bright band thin); down: horizon to ground.
            vec3 color = dir.y >= 0.0
                ? mix(uHorizon, uZenith, pow(dir.y, 0.5))
                : mix(uHorizon, uGround, clamp(-dir.y * 4.0, 0.0, 1.0));

            // The sun: a tight disc plus a wide soft glow, only above the horizon.
            float s = max(dot(dir, uToSun), 0.0);
            color += uSunColor * (pow(s, 900.0) * 3.0 + pow(s, 14.0) * 0.18) * step(0.0, dir.y + 0.05);
            fragColor = vec4(color, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly FullscreenQuad _quad;
    private readonly Shader _shader;

    /// <summary>Creates the sky shader. Needs a current GL context.</summary>
    public SkyRenderer(GL gl)
    {
        _gl = gl;
        _quad = new FullscreenQuad(gl);
        _shader = new Shader(gl, FullscreenQuad.VertexSource, Fragment);
    }

    /// <summary>Draws the sky over the whole target. Depth testing and writing are left off, so draw the world over it afterwards.</summary>
    /// <param name="camera">The view the sky is seen from.</param>
    /// <param name="lighting">Zenith, horizon and ground colours.</param>
    /// <param name="towardSun">Unit vector from the scene to the sun (opposite the sun light's travel direction).</param>
    /// <param name="sunColor">Colour of the sun disc; zero draws none (a scene without a sun).</param>
    public void Draw(Camera3D camera, Lighting3D lighting, Vector3 towardSun, Vector3 sunColor)
    {
        if (!Matrix4x4.Invert(camera.GetViewProjection(), out var inverse)) return; // a degenerate camera: leave the clear colour

        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.Blend);
        _shader.Use();
        _shader.SetUniform("uInverseViewProjection", inverse);
        _shader.SetUniform("uCameraPosition", camera.Position);
        _shader.SetUniform("uZenith", ToVector(lighting.SkyZenith));
        _shader.SetUniform("uHorizon", ToVector(lighting.SkyHorizon));
        _shader.SetUniform("uGround", ToVector(lighting.ReflectedGround));
        _shader.SetUniform("uToSun", Vector3.Normalize(towardSun));
        _shader.SetUniform("uSunColor", sunColor);
        _quad.Draw();
        _gl.BindVertexArray(0);
    }

    private static Vector3 ToVector(Color color) => new(color.R, color.G, color.B);

    /// <summary>Frees the shader and the vertex array.</summary>
    public void Dispose()
    {
        _shader.Dispose();
        _quad.Dispose();
    }
}
