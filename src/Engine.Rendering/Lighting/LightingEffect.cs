using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// 2D lighting as a post effect. It builds a "light map" the size of the screen (cleared to the <see cref="Ambient"/> colour, then
/// every <see cref="Light2D"/> in view added on top as a soft glowing disc), and multiplies the scene by it: dark where no light
/// reaches, full colour where light is strong. Put it first in the chain, before bloom, so the glow can bloom.
/// </summary>
/// <remarks>
/// Why a light map multiply and not per-sprite lighting: lights cost the same however many sprites there are, and the sprite
/// renderer needs no changes. Limits: no shadows (light passes through walls) and no normal maps.
/// </remarks>
public sealed class LightingEffect : IPostEffect
{
    private const int GradientSize = 128;

    private const string CompositeFragment = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform sampler2D uLightMap;
        uniform float uExposure;
        uniform int uClamp;
        out vec4 FragColor;

        void main()
        {
            vec3 scene = texture(uTexture, vUv).rgb;
            vec3 light = texture(uLightMap, vUv).rgb * uExposure;
            vec3 lit = scene * light;
            FragColor = vec4(uClamp == 1 ? min(lit, vec3(1.0)) : lit, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly World _world;
    private readonly Camera2D _camera;
    private readonly RenderClock? _clock;
    private readonly SpriteBatch _batch;
    private readonly Texture2D _gradient;
    private readonly Shader _composite;
    private readonly EntityAction<Transform, Light2D> _collect;
    private RenderTarget? _lightMap;
    private Vector2 _visibleMin;
    private Vector2 _visibleMax;
    private int _lightsDrawn;

    /// <summary>Creates the effect. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the effect's resources.</param>
    /// <param name="world">The world whose <see cref="Light2D"/> entities are drawn.</param>
    /// <param name="camera">The scene camera; the light map is drawn through the same view so lights line up with the sprites.</param>
    /// <param name="clock">Interpolation factor; when given, lights on entities with a <see cref="PreviousTransform"/> follow them smoothly between fixed steps, like their sprites.</param>
    public LightingEffect(GL gl, World world, Camera2D camera, RenderClock? clock = null)
    {
        _clock = clock;
        _gl = gl;
        _world = world;
        _camera = camera;
        _batch = new SpriteBatch(gl);
        _gradient = Texture2D.FromPixels(gl, GradientSize, GradientSize, LightMath.BuildGradientPixels(GradientSize), TextureFilter.Linear);
        _composite = new Shader(gl, FullscreenQuad.VertexSource, CompositeFragment);
        _collect = Collect; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public string Name => "Lighting";

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <summary>Light level where no light reaches: black is pitch dark, white leaves the scene unlit-looking (no darkening).</summary>
    public Vector3 Ambient { get; set; } = new(0.3f, 0.32f, 0.45f);

    /// <summary>Multiplies the whole light map after it is built; a global brightness control for the lights.</summary>
    public float Exposure { get; set; } = 1f;

    /// <summary>How many lights were drawn last frame (those outside the view are skipped); shown by tools.</summary>
    public int LightsDrawn => _lightsDrawn;

    /// <inheritdoc />
    public void Apply(PostProcessContext context, Texture2D source, RenderTarget? destination)
    {
        // The light map must be as precise as the chain: in an HDR chain, lights add up past 1 instead of clipping.
        if (_lightMap == null || _lightMap.Format.Hdr != context.IsHdr)
        {
            _lightMap?.Dispose();
            _lightMap = new RenderTarget(_gl, source.Width, source.Height, new RenderTargetFormat(Hdr: context.IsHdr));
        }

        _lightMap.Resize(source.Width, source.Height);
        BuildLightMap();

        context.BindDestination(destination);
        _composite.Use();
        source.Bind(0);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _lightMap.Texture.Handle);
        _composite.SetUniform("uTexture", 0);
        _composite.SetUniform("uLightMap", 1);
        _composite.SetUniform("uExposure", Exposure);
        _composite.SetUniform("uClamp", context.IsHdr ? 0 : 1);
        context.Quad.Draw();
        _gl.ActiveTexture(TextureUnit.Texture0);
    }

    /// <summary>Releases the light map, light sprite, batch and shader.</summary>
    public void Dispose()
    {
        _lightMap?.Dispose();
        _gradient.Dispose();
        _batch.Dispose();
        _composite.Dispose();
    }

    private void BuildLightMap()
    {
        _lightMap!.Bind();
        _gl.ClearColor(Ambient.X, Ambient.Y, Ambient.Z, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _camera.GetVisibleBounds(out _visibleMin, out _visibleMax);
        _lightsDrawn = 0;
        _batch.Begin(_camera.GetViewProjection(), SpriteBlend.Additive);
        _world.Query(_collect);
        _batch.End();
    }

    private void Collect(Entity entity, ref Transform simulated, ref Light2D light)
    {
        if (light.Radius <= 0f) return;

        var transform = TransformHierarchy.GetRenderTransform(_world, entity, simulated, _clock);

        var centre = new Vector2(transform.Position.X, transform.Position.Y);
        if (!LightMath.Overlaps(_visibleMin, _visibleMax, centre, light.Radius)) return;

        var draw = SpriteDraw.At(transform.Position, new Vector2(light.Radius * 2f));
        draw.Color = new Color(light.Color.R * light.Intensity, light.Color.G * light.Intensity, light.Color.B * light.Intensity);
        _batch.Draw(_gradient, draw);
        _lightsDrawn++;
    }
}
