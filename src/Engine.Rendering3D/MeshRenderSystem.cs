using System.Numerics;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Rendering;
using Silk.NET.OpenGL;
using Shader = Engine.Rendering.Shader;

namespace Engine.Rendering3D;

/// <summary>
/// Draws every entity that has a <see cref="Transform"/> and a <see cref="MeshRenderer"/> or <see cref="ModelRenderer"/> with depth testing and
/// metallic-roughness (PBR) lighting from the scene's <see cref="DirectionalLight"/> and <see cref="PointLight3D"/> components. Add it to
/// <c>RenderSystems</c> before the 2D passes: it clears the depth buffer first, draws the 3D world, and leaves the depth test off so sprites and
/// tiles drawn afterwards (a HUD, 2D effects) land on top. Owns a shader, an instance buffer and a white texture, so dispose it in <c>Unload</c>.
/// </summary>
/// <remarks>
/// <para>Per frame: gather lights, gather a list of visible meshes (frustum culled), sort it, then draw opaque and cut-out surfaces grouped by
/// material and mesh (runs of the same mesh and material are one instanced call), then blended surfaces farthest first without writing depth.</para>
/// <para>The sun casts shadows into a depth map drawn first (<c>MeshRenderSystem.Shadows.cs</c>), which the main shader samples.</para>
/// <para>Split by topic: this file (setup and the frame), <c>MeshRenderSystem.Items.cs</c> (gathering and sorting), <c>MeshRenderSystem.Draw.cs</c>
/// (passes, materials, instancing), <c>MeshRenderSystem.Shadows.cs</c> (the shadow pass) and <c>MeshRenderSystem.Lights.cs</c> (lights).</para>
/// </remarks>
public sealed partial class MeshRenderSystem : ISystem, IDisposable
{
    private static readonly Material DefaultMaterial = new();

    private readonly GL _gl;
    private readonly AssetManager _assets;
    private readonly Camera3D _camera;
    private readonly RenderClock? _clock;
    private readonly ModelPoses? _poses;
    private readonly Shader _shader;
    private readonly InstanceBuffer _instances;
    private readonly Texture2D _white;
    private readonly Shader _shadowShader;
    private readonly ShadowMap _shadowMap;
    private readonly EntityAction<Transform, MeshRenderer> _drawEntity;
    private readonly EntityAction<Transform, ModelRenderer> _drawModelEntity;
    private readonly EntityAction<Transform, DirectionalLight> _collectSun;
    private readonly EntityAction<Transform, PointLight3D> _collectPoint;
    private readonly int[] _chosen = new int[MeshShaderSource.MaxPointLights];

    private World _world = null!;
    private Frustum _frustum;
    private Material? _boundMaterial;
    private bool _cullEnabled;

    /// <summary>Creates the system. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the shader.</param>
    /// <param name="assets">Resolves mesh, material and texture handles.</param>
    /// <param name="camera">The camera that defines the view; keep its <see cref="Camera3D.ViewportSize"/> up to date.</param>
    /// <param name="clock">Interpolation factor; when given, entities with a <see cref="PreviousTransform"/> are drawn between their last two fixed steps.</param>
    /// <param name="shadowMapResolution">Side of the square sun shadow map in texels; 2048 is a good default, 4096 is crisper and costs four times the memory.</param>
    /// <param name="poses">The animated poses written by <see cref="ModelAnimationSystem"/>; null draws every model at rest.</param>
    public MeshRenderSystem(GL gl, AssetManager assets, Camera3D camera, RenderClock? clock = null, int shadowMapResolution = 2048, ModelPoses? poses = null)
    {
        _gl = gl;
        _assets = assets;
        _camera = camera;
        _clock = clock;
        _poses = poses;
        _shader = new Shader(gl, MeshShaderSource.Vertex, MeshShaderSource.Fragment);
        _instances = new InstanceBuffer(gl);
        _white = Texture2D.CreateWhite(gl);
        _shadowShader = new Shader(gl, ShadowShaderSource.Vertex, ShadowShaderSource.Fragment);
        _shadowMap = new ShadowMap(gl, shadowMapResolution);
        _drawEntity = GatherEntity; // created once so the per-frame queries do not allocate delegates
        _drawModelEntity = GatherModelEntity;
        _collectSun = CollectSun;
        _collectPoint = CollectPoint;
        InitialiseLights();
    }

    /// <summary>Scene-wide lighting settings (ambient colour).</summary>
    public Lighting3D Lighting { get; } = new();

    /// <summary>Meshes drawn in the last frame (after frustum culling).</summary>
    public int DrawnLastFrame { get; private set; }

    /// <summary>Meshes skipped in the last frame because their bounding sphere was outside the view.</summary>
    public int CulledLastFrame { get; private set; }

    /// <summary>Draw calls issued in the last frame: fewer than <see cref="DrawnLastFrame"/> when identical meshes were instanced.</summary>
    public int DrawCallsLastFrame { get; private set; }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _world = world;
        DrawnLastFrame = 0;
        CulledLastFrame = 0;
        DrawCallsLastFrame = 0;

        GatherLights(world);

        // The depth buffer is cleared here, not by the game, so a game that mixes 2D and 3D needs no extra call; it must be writable first.
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Less);
        _gl.DepthMask(true);
        _gl.Clear(ClearBufferMask.DepthBufferBit);
        _gl.Disable(EnableCap.Blend);
        _gl.Disable(EnableCap.CullFace); // each material switches it on or off as it is bound
        _gl.FrontFace(FrontFaceDirection.Ccw);

        _frustum = _camera.GetFrustum();
        PrepareShadows();
        GatherItems(world);
        RenderShadowMap(); // before the main shader is set up: it switches framebuffer and shader, and puts both back

        _shader.Use();
        _shader.SetUniform("uViewProjection", _camera.GetViewProjection());
        _shader.SetUniform("uCameraPosition", _camera.Position);
        _shader.SetUniform("uBaseTexture", 0);
        _shader.SetUniform("uMetallicRoughnessTexture", 1);
        _shader.SetUniform("uNormalTexture", 2);
        UploadSceneLights();
        UploadShadowUniforms();
        DrawItems();

        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.DepthTest);
        _gl.BindVertexArray(0);
    }

    /// <summary>Deletes the shader, the instance buffer and the white texture.</summary>
    public void Dispose()
    {
        _shader.Dispose();
        _instances.Dispose();
        _white.Dispose();
        _shadowShader.Dispose();
        _shadowMap.Dispose();
    }
}
