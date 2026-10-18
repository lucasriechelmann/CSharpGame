using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Platform;
using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Runtime;

/// <summary>
/// Base class for a game. Subclass it, override the hooks, and call <see cref="Run"/>.
/// Simulation runs at a fixed rate; rendering runs once per display frame.
/// </summary>
public abstract class Game : IDisposable
{
    private readonly GameWindow _window;
    private readonly FixedTimestep _timestep;
    private readonly FrameStats _stats = new();
    private readonly DebugOverlay _overlay;

    /// <summary>Creates the window and loop. Nothing is shown until <see cref="Run"/> is called.</summary>
    /// <param name="title">Window title.</param>
    /// <param name="width">Initial window width in pixels.</param>
    /// <param name="height">Initial window height in pixels.</param>
    /// <param name="fixedHz">Fixed simulation rate in steps per second.</param>
    protected Game(string title, int width = 1280, int height = 720, double fixedHz = 60)
    {
        _timestep = new FixedTimestep(1.0 / fixedHz);
        _window = new GameWindow(title, width, height);
        Actions = new ActionMap(_window.Input);
        _overlay = new DebugOverlay(_window, _timestep, _stats, World, Assets);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Gui += OnGui;
    }

    /// <summary>The entity world for this game.</summary>
    public World World { get; } = new();

    /// <summary>
    /// Loads and caches assets from the <c>Assets</c> folder next to the executable. A texture loader is registered
    /// automatically once the window is ready; everything it owns is disposed on shutdown.
    /// </summary>
    public AssetManager Assets { get; } = new(Path.Combine(AppContext.BaseDirectory, "Assets"));

    /// <summary>Raw keyboard and mouse state. Prefer <see cref="Actions"/> for game logic.</summary>
    public InputState Input => _window.Input;

    /// <summary>Named actions and axes mapped onto <see cref="Input"/>. Bind them in the constructor or <see cref="Load"/>.</summary>
    public ActionMap Actions { get; }

    /// <summary>Systems run once per fixed simulation step (physics, movement, game logic).</summary>
    public SystemGroup FixedSystems { get; } = new();

    /// <summary>Systems run once per frame, before the fixed steps (input handling, animation, camera smoothing).</summary>
    public SystemGroup FrameSystems { get; } = new();

    /// <summary>Systems run each frame after <see cref="Draw"/> (e.g. the sprite renderer), in the order added.</summary>
    public SystemGroup RenderSystems { get; } = new();

    /// <summary>The OpenGL API. Only valid after the window has loaded, i.e. from <see cref="Load"/> onwards.</summary>
    protected GL Gl => _window.Gl;

    /// <summary>The platform window (size, VSync, resize events).</summary>
    protected GameWindow Window => _window;

    /// <summary>Length of one fixed simulation step, in seconds.</summary>
    protected double FixedDeltaTime => _timestep.Step;

    /// <summary>0..1 position between the last two fixed steps, for render interpolation.</summary>
    protected double Alpha => _timestep.Alpha;

    /// <summary>Called once after the window and OpenGL context exist. Create GPU resources here.</summary>
    protected virtual void Load() { }

    /// <summary>Called once when the game shuts down, before the GL context is destroyed. Dispose GPU resources created in <see cref="Load"/> here.</summary>
    protected virtual void Unload() { }

    /// <summary>Called once per fixed simulation step, after <see cref="FixedSystems"/> have run.</summary>
    /// <param name="dt">The fixed step length in seconds.</param>
    protected virtual void FixedUpdate(double dt) { }

    /// <summary>Called once per frame, after <see cref="FrameSystems"/> and before the fixed steps.</summary>
    /// <param name="dt">Seconds since the previous frame.</param>
    protected virtual void Update(double dt) { }

    /// <summary>Called once per frame to prepare the frame (e.g. clear the screen); <see cref="RenderSystems"/> run right after it, then the debug UI is drawn on top.</summary>
    /// <param name="dt">Seconds since the previous frame.</param>
    protected virtual void Draw(double dt) { }

    /// <summary>Game-specific debug panels (ImGui calls). Hidden together with the engine overlay (F1).</summary>
    protected virtual void DebugGui() { }

    /// <summary>Opens the window and runs the game loop until the window closes.</summary>
    public void Run() => _window.Run();

    /// <summary>Releases game resources (<see cref="Unload"/>, then every asset) and finally the window and GPU context.</summary>
    public void Dispose()
    {
        Unload();
        Assets.Dispose();
        _window.Dispose();
    }

    private void OnLoad()
    {
        Assets.Register(new TextureLoader(Gl));
        Load();
    }

    /// <summary>
    /// One frame of logic: per-frame code first (so input is read once and latched into components), then the
    /// fixed simulation steps that consume it, then input edges are cleared.
    /// </summary>
    private void OnUpdate(double dt)
    {
        if (Input.WasPressed(KeyCode.F1)) _overlay.Toggle();

        FrameSystems.Run(World, dt);
        Update(dt);

        int steps = _timestep.Advance(dt);
        for (int i = 0; i < steps; i++)
        {
            FixedSystems.Run(World, _timestep.Step);
            FixedUpdate(_timestep.Step);
        }

        _stats.RecordFixedSteps(steps);
        Input.EndFrame();
    }

    private void OnRender(double dt)
    {
        _stats.RecordFrame(dt);
        Draw(dt);
        RenderSystems.Run(World, dt);
    }

    private void OnGui(double dt)
    {
        if (!_overlay.Visible) return;

        _overlay.Draw();
        DebugGui();
    }
}
