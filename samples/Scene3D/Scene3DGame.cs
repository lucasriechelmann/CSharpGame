using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Physics3D;
using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Runtime;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace Scene3D;

/// <summary>
/// The 3D sample: a lit room (floor, spinning cubes, spheres, a sun and two point lights) loaded from a scene file and viewed
/// with a fly camera. Hold the right mouse button and move the mouse to look; W/A/S/D fly, Q/E go down/up, Shift is faster.
/// </summary>
/// <remarks>
/// The content is data (<c>Assets/scenes/room.scene.json</c> and the <c>.material.json</c> files); the code only creates what
/// a file cannot hold, the procedural meshes and the checker texture, and registers them under the names the scene uses.
/// Space throws a crate (a <see cref="PhysicsBody3D"/> that falls onto the floor and the platform); the left mouse button casts a
/// ray from the cursor, moves a marker to where it hits a static solid and logs what was hit.
/// </remarks>
public sealed class Scene3DGame : Game
{
    private const float LookSensitivity = 0.0035f; // radians per pixel of mouse movement
    private const float FlySpeed = 5f;
    private const float FastMultiplier = 3f;
    private const float ThrowSpeed = 7f;
    private const int MaxCrates = 30;

    private readonly Camera3D _camera = new();
    private readonly FlyCamera _fly;
    private readonly Queue<Entity> _crates = new();
    private MeshRenderSystem _meshes = null!;
    private PhysicsSystem3D _physics = null!;
    private Entity _marker;
    private int _thrown;
    private string _lastPick = "(left-click the world)";
    private Vector2 _lastMouse;
    private bool _wasLooking;

    /// <summary>Registers input bindings and the game's own component.</summary>
    public Scene3DGame() : base("Scene3D")
    {
        Actions
            .BindAxis(GameActions.MoveForward, KeyCode.S, KeyCode.W)
            .BindAxis(GameActions.MoveRight, KeyCode.A, KeyCode.D)
            .BindAxis(GameActions.MoveUp, KeyCode.Q, KeyCode.E)
            .Bind(GameActions.LookHeld, MouseButton.Right)
            .Bind(GameActions.Drop, KeyCode.Space)
            .Bind(GameActions.Pick, MouseButton.Left)
            .Bind(GameActions.Fast, KeyCode.ShiftLeft);

        Components.Register<Spin>("Spin");

        _camera.Position = new Vector3(0f, 3.5f, 9f);
        _camera.LookAt(new Vector3(0f, 1f, 0f));
        _fly = FlyCamera.From(_camera);
    }

    /// <inheritdoc />
    protected override void Load()
    {
        // Procedural assets first: the scene and the materials refer to them by name.
        Assets.Add("meshes/cube", new Mesh(Gl, MeshBuilder.Cube(Vector3.One)));
        Assets.Add("meshes/sphere", new Mesh(Gl, MeshBuilder.Sphere(0.5f)));
        Assets.Add("meshes/floor", new Mesh(Gl, MeshBuilder.Plane(40f, 40f, uvRepeat: 20f)));
        Assets.Add("checker", CreateChecker());

        Scenes.LoadFile("scenes/room.scene.json");

        _camera.ViewportSize = new Vector2(Window.Width, Window.Height);
        Window.Resized += (w, h) => _camera.ViewportSize = new Vector2(w, h);

        _marker = FindByName("Pick marker");
        FrameSystems.Add(new SpinSystem());

        // Fixed step: record where everything was (for render interpolation), then let physics move the crates.
        _physics = new PhysicsSystem3D();
        FixedSystems.Add(new PreviousTransformSystem());
        FixedSystems.Add(_physics);
        _meshes = new MeshRenderSystem(Gl, Assets, _camera, RenderClock);
        RenderSystems.Add(_meshes);

        // With an effect on, the scene is drawn into an off-screen target: a check that depth testing works there too.
        PostProcess.Add(new VignetteEffect(Gl) { Enabled = false });
        Log.Info("Game", $"Room loaded: {World.EntityCount} entities");
    }

    /// <inheritdoc />
    protected override Camera3D? SceneCamera3D => _camera;

    /// <inheritdoc />
    protected override string? ScenePath => FindProjectScene();

    /// <inheritdoc />
    protected override void SceneReloaded()
    {
        // The world was rebuilt (editor Reload or Stop): every entity handle cached from the old one is stale.
        _marker = FindByName("Pick marker");
        _crates.Clear();
    }

    /// <inheritdoc />
    protected override void Unload() => _meshes.Dispose();

    /// <summary>
    /// The scene file in the project's <c>Assets</c> folder (not the copy beside the executable), found by walking up from the executable,
    /// so the editor's Save changes something that can be committed. Null when the executable runs outside the project tree.
    /// </summary>
    private static string? FindProjectScene()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "samples", "Scene3D", "Assets", "scenes", "room.scene.json");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <inheritdoc />
    protected override void Update(double dt)
    {
        if (IsEditing) return; // the scene editor (F2) owns the mouse

        // Look only while the right button is held; the first frame just records where the mouse is, so there is no jump.
        bool looking = Actions.IsDown(GameActions.LookHeld);
        var mouse = Input.MousePosition;
        if (looking && _wasLooking)
        {
            var delta = mouse - _lastMouse;
            _fly.Look(-delta.X * LookSensitivity, -delta.Y * LookSensitivity); // mouse right = turn right = negative yaw
        }

        _wasLooking = looking;
        _lastMouse = mouse;

        if (Actions.WasPressed(GameActions.Drop)) ThrowCrate();
        if (Actions.WasPressed(GameActions.Pick)) PickWithRay();

        float speed = FlySpeed * (Actions.IsDown(GameActions.Fast) ? FastMultiplier : 1f);
        _fly.Move(
            Actions.GetAxis(GameActions.MoveForward),
            Actions.GetAxis(GameActions.MoveRight),
            Actions.GetAxis(GameActions.MoveUp),
            speed * (float)dt);
    }

    /// <summary>Spawns a crate just in front of the camera and throws it along the view direction with a little lift.</summary>
    private void ThrowCrate()
    {
        var crate = Scenes.Instantiate("prefabs/crate.prefab.json");
        var forward = _camera.Forward;
        var transform = World.Get<Transform>(crate);
        transform.Position = _camera.Position + forward * 1.5f;
        World.Set(crate, transform);
        // Each crate is thrown a little to the side of the last, so they do not land in the same spot (bodies do not collide with each other yet).
        var sideways = _camera.Right * (_thrown++ % 3 - 1) * 1.5f;
        World.Set(crate, new PhysicsBody3D { Velocity = forward * ThrowSpeed + sideways + Vector3.UnitY * 2f, GravityScale = 1f });

        _crates.Enqueue(crate);
        if (_crates.Count > MaxCrates) World.DestroyEntity(_crates.Dequeue()); // keep the pile bounded
    }

    /// <summary>Casts a ray through the cursor, moves the marker to the hit point and remembers a description for the panel and the log.</summary>
    private void PickWithRay()
    {
        var ray = _camera.ScreenPointToRay(Input.MousePosition);
        if (!_physics.Raycast(ray, 100f, out var hit, out var entity))
        {
            _lastPick = "Ray hit nothing";
            return;
        }

        string name = World.TryGet<Name>(entity, out var label) ? label.Value : "(unnamed)";
        _lastPick = $"Ray hit {name} at {hit.Distance:F1} m, face normal ({hit.Normal.X:F0}, {hit.Normal.Y:F0}, {hit.Normal.Z:F0})";
        Log.Info("Game", _lastPick);

        var marker = World.Get<Transform>(_marker);
        marker.Position = hit.Point;
        World.Set(_marker, marker);
    }

    private Entity FindByName(string name)
    {
        Entity found = Entity.Null;
        World.Query((Entity entity, ref Name label) =>
        {
            if (label.Value == name) found = entity;
        });
        return found.IsNull ? throw new InvalidOperationException($"The scene has no entity named '{name}'.") : found;
    }

    /// <inheritdoc />
    protected override void Draw(double dt)
    {
        // Applied here, after Update, so the picture uses this frame's input; the depth buffer is cleared by the mesh system.
        // While the editor is open it owns the camera (orbit view), so follow it instead: flying on afterwards starts where it ended.
        if (IsEditing) _fly.CopyFrom(_camera);
        else _fly.ApplyTo(_camera);
        Gl.ClearColor(0.45f, 0.62f, 0.85f, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    /// <inheritdoc />
    protected override void DebugGui()
    {
        ImGui.SetNextWindowPos(new Vector2(12f, 120f), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("3D scene"))
        {
            ImGui.Text($"Meshes drawn: {_meshes.DrawnLastFrame}  culled: {_meshes.CulledLastFrame}");
            ImGui.Text($"Camera: {_fly.Position.X:F1}, {_fly.Position.Y:F1}, {_fly.Position.Z:F1}");
            var ambient = new Vector3(_meshes.Lighting.Ambient.R, _meshes.Lighting.Ambient.G, _meshes.Lighting.Ambient.B);
            if (ImGui.ColorEdit3("Ambient", ref ambient)) _meshes.Lighting.Ambient = new Color(ambient.X, ambient.Y, ambient.Z);
            float fovDegrees = _camera.FovY * 180f / MathF.PI;
            if (ImGui.SliderFloat("Field of view", ref fovDegrees, 20f, 110f)) _camera.FovY = fovDegrees * MathF.PI / 180f;
            ImGui.Text($"Crates: {_crates.Count}   {_lastPick}");
            ImGui.TextDisabled("Right mouse + move: look.  W/A/S/D: fly.  Q/E: down/up.  Shift: fast.");
            ImGui.TextDisabled("Space: throw a crate.  Left click: cast a ray.");
        }

        ImGui.End();
    }

    /// <summary>A grey two-tone checker (64x64 pixels, 8-pixel squares) that tiles and is mipmapped so the far floor does not shimmer.</summary>
    private Engine.Rendering.Texture2D CreateChecker()
    {
        const int size = 64;
        const int square = 8;
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool light = ((x / square) + (y / square)) % 2 == 0;
                byte value = light ? (byte)230 : (byte)120;
                int i = (y * size + x) * 4;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
        }

        var texture = Engine.Rendering.Texture2D.FromPixels(Gl, size, size, pixels, TextureFilter.Trilinear);
        texture.SetRepeat();
        return texture;
    }
}
