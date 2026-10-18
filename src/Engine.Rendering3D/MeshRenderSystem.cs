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
/// Draws every entity that has a <see cref="Transform"/> and a <see cref="MeshRenderer"/> with depth testing and Blinn-Phong lighting
/// from the scene's <see cref="DirectionalLight"/> and <see cref="PointLight3D"/> components. Add it to <c>RenderSystems</c> before the
/// 2D passes: it clears the depth buffer first, draws the 3D world, and leaves the depth test off so sprites and tiles drawn afterwards
/// (a HUD, 2D effects) land on top. Opaque only: there is no sorting or transparency yet. Owns a shader and a white texture, so dispose
/// it in <c>Unload</c>.
/// </summary>
public sealed partial class MeshRenderSystem : ISystem, IDisposable
{
    private static readonly Material DefaultMaterial = new();

    private readonly GL _gl;
    private readonly AssetManager _assets;
    private readonly Camera3D _camera;
    private readonly RenderClock? _clock;
    private readonly Shader _shader;
    private readonly Texture2D _white;
    private readonly EntityAction<Transform, MeshRenderer> _drawEntity;
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
    public MeshRenderSystem(GL gl, AssetManager assets, Camera3D camera, RenderClock? clock = null)
    {
        _gl = gl;
        _assets = assets;
        _camera = camera;
        _clock = clock;
        _shader = new Shader(gl, MeshShaderSource.Vertex, MeshShaderSource.Fragment);
        _white = Texture2D.CreateWhite(gl);
        _drawEntity = DrawEntity; // created once so the per-frame queries do not allocate delegates
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

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _world = world;
        DrawnLastFrame = 0;
        CulledLastFrame = 0;

        GatherLights(world);

        // The depth buffer is cleared here, not by the game, so a game that mixes 2D and 3D needs no extra call; it must be writable first.
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Less);
        _gl.DepthMask(true);
        _gl.Clear(ClearBufferMask.DepthBufferBit);
        _gl.Disable(EnableCap.Blend);
        _gl.Disable(EnableCap.CullFace); // each material switches it on or off as it is bound
        _gl.FrontFace(FrontFaceDirection.Ccw);

        _shader.Use();
        _shader.SetUniform("uViewProjection", _camera.GetViewProjection());
        _shader.SetUniform("uCameraPosition", _camera.Position);
        _shader.SetUniform("uTexture", 0);
        UploadSceneLights();

        _frustum = _camera.GetFrustum();
        _boundMaterial = null;
        _cullEnabled = false;
        world.Query(_drawEntity);

        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.DepthTest);
        _gl.BindVertexArray(0);
    }

    /// <summary>Deletes the shader and the white texture.</summary>
    public void Dispose()
    {
        _shader.Dispose();
        _white.Dispose();
    }

    private void DrawEntity(Entity entity, ref Transform simulated, ref MeshRenderer renderer)
    {
        if (!renderer.Mesh.IsValid || !_assets.TryGet(renderer.Mesh, out var mesh)) return;

        var transform = simulated;
        if (_clock != null && _world.TryGet<PreviousTransform>(entity, out var previous))
            transform = PreviousTransform.Interpolate(simulated, previous, _clock.Alpha);

        var model = ModelMatrix.From(transform);
        var centre = Vector3.Transform(mesh.BoundsCenter, model);
        float radius = mesh.BoundsRadius * ModelMatrix.MaxScale(model);
        if (!_frustum.IntersectsSphere(centre, radius))
        {
            CulledLastFrame++;
            return;
        }

        var material = renderer.Material.IsValid && _assets.TryGet(renderer.Material, out var found) ? found : DefaultMaterial;
        BindMaterial(material);

        _shader.SetUniform("uModel", model);
        _shader.SetUniform("uNormalMatrix", ModelMatrix.NormalMatrix(model));
        UploadPointLights(centre, radius);
        mesh.Draw();
        DrawnLastFrame++;
    }

    private void BindMaterial(Material material)
    {
        // Consecutive meshes usually share a material, so skip the uniform and texture traffic when nothing changed.
        // (A material edited between two draws in one frame is not a case worth handling.)
        if (ReferenceEquals(material, _boundMaterial)) return;
        _boundMaterial = material;

        var texture = material.Texture.IsValid && _assets.TryGet(material.Texture, out var found) ? found : _white;
        texture.Bind(0);
        var color = material.BaseColor;
        _shader.SetUniform("uBaseColor", new Vector4(color.R, color.G, color.B, color.A));
        _shader.SetUniform("uShininess", material.Shininess);
        _shader.SetUniform("uSpecular", material.Specular);
        _shader.SetUniform("uUnlit", material.Unlit ? 1 : 0);

        bool cull = !material.DoubleSided;
        if (cull)
        {
            if (!_cullEnabled) _gl.Enable(EnableCap.CullFace);
            _gl.CullFace(TriangleFace.Back);
        }
        else if (_cullEnabled)
        {
            _gl.Disable(EnableCap.CullFace);
        }

        _cullEnabled = cull;
    }
}
