using System.Numerics;
using Engine.Core.Input;
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
/// </remarks>
public sealed class Scene3DGame : Game
{
    private const float LookSensitivity = 0.0035f; // radians per pixel of mouse movement
    private const float FlySpeed = 5f;
    private const float FastMultiplier = 3f;

    private readonly Camera3D _camera = new();
    private readonly FlyCamera _fly;
    private MeshRenderSystem _meshes = null!;
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

        FrameSystems.Add(new SpinSystem());
        _meshes = new MeshRenderSystem(Gl, Assets, _camera, RenderClock);
        RenderSystems.Add(_meshes);

        // With an effect on, the scene is drawn into an off-screen target: a check that depth testing works there too.
        PostProcess.Add(new VignetteEffect(Gl) { Enabled = false });
        Log.Info("Game", $"Room loaded: {World.EntityCount} entities");
    }

    /// <inheritdoc />
    protected override void Unload() => _meshes.Dispose();

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

        float speed = FlySpeed * (Actions.IsDown(GameActions.Fast) ? FastMultiplier : 1f);
        _fly.Move(
            Actions.GetAxis(GameActions.MoveForward),
            Actions.GetAxis(GameActions.MoveRight),
            Actions.GetAxis(GameActions.MoveUp),
            speed * (float)dt);
    }

    /// <inheritdoc />
    protected override void Draw(double dt)
    {
        // Applied here, after Update, so the picture uses this frame's input; the depth buffer is cleared by the mesh system.
        _fly.ApplyTo(_camera);
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
            ImGui.TextDisabled("Right mouse + move: look.  W/A/S/D: fly.  Q/E: down/up.  Shift: fast.");
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
