using Engine.Core;
using Engine.Core.Ecs;
using Engine.Platform;
using Silk.NET.Input;
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
        _overlay = new DebugOverlay(_window, _timestep, _stats, World);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Gui += OnGui;
    }

    /// <summary>The entity world for this game.</summary>
    public World World { get; } = new();

    /// <summary>Systems run once per fixed simulation step (physics, movement, game logic).</summary>
    public SystemGroup FixedSystems { get; } = new();

    /// <summary>Systems run once per frame (input handling, animation, camera smoothing).</summary>
    public SystemGroup FrameSystems { get; } = new();

    /// <summary>The OpenGL API. Only valid after the window has loaded, i.e. from <see cref="Load"/> onwards.</summary>
    protected GL Gl => _window.Gl;

    /// <summary>The platform window (size, input, VSync).</summary>
    protected GameWindow Window => _window;

    /// <summary>Length of one fixed simulation step, in seconds.</summary>
    protected double FixedDeltaTime => _timestep.Step;

    /// <summary>0..1 position between the last two fixed steps, for render interpolation.</summary>
    protected double Alpha => _timestep.Alpha;

    /// <summary>Called once after the window and OpenGL context exist. Create GPU resources here.</summary>
    protected virtual void Load() { }

    /// <summary>Called once per fixed simulation step, after <see cref="FixedSystems"/> have run.</summary>
    /// <param name="dt">The fixed step length in seconds.</param>
    protected virtual void FixedUpdate(double dt) { }

    /// <summary>Called once per frame, after <see cref="FrameSystems"/> have run.</summary>
    /// <param name="dt">Seconds since the previous frame.</param>
    protected virtual void Update(double dt) { }

    /// <summary>Called once per frame to draw the scene, before the debug UI is drawn on top.</summary>
    /// <param name="dt">Seconds since the previous frame.</param>
    protected virtual void Draw(double dt) { }

    /// <summary>Game-specific debug panels (ImGui calls). Hidden together with the engine overlay (F1).</summary>
    protected virtual void DebugGui() { }

    /// <summary>Opens the window and runs the game loop until the window closes.</summary>
    public void Run() => _window.Run();

    /// <summary>Releases the window and GPU context.</summary>
    public void Dispose() => _window.Dispose();

    private void OnLoad()
    {
        _window.Input.Keyboards[0].KeyDown += (_, key, _) =>
        {
            if (key == Key.F1) _overlay.Toggle();
        };
        Load();
    }

    private void OnUpdate(double dt)
    {
        int steps = _timestep.Advance(dt);
        for (int i = 0; i < steps; i++)
        {
            FixedSystems.Run(World, _timestep.Step);
            FixedUpdate(_timestep.Step);
        }

        _stats.RecordFixedSteps(steps);
        FrameSystems.Run(World, dt);
        Update(dt);
    }

    private void OnRender(double dt)
    {
        _stats.RecordFrame(dt);
        Draw(dt);
    }

    private void OnGui(double dt)
    {
        if (!_overlay.Visible) return;

        _overlay.Draw();
        DebugGui();
    }
}
