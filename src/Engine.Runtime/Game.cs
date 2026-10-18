using Engine.Core;
using Engine.Platform;
using ImGuiNET;
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
    private bool _showDebug = true;

    protected Game(string title, int width = 1280, int height = 720, double fixedHz = 60)
    {
        _timestep = new FixedTimestep(1.0 / fixedHz);
        _window = new GameWindow(title, width, height);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Gui += OnGui;
    }

    protected GL Gl => _window.Gl;
    protected GameWindow Window => _window;
    protected double FixedDeltaTime => _timestep.Step;

    /// <summary>0..1 position between the last two fixed steps, for render interpolation.</summary>
    protected double Alpha => _timestep.Alpha;

    protected virtual void Load() { }
    protected virtual void FixedUpdate(double dt) { }
    protected virtual void Update(double dt) { }
    protected virtual void Draw(double dt) { }

    /// <summary>Game-specific debug panels (ImGui calls).</summary>
    protected virtual void DebugGui() { }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        _window.Input.Keyboards[0].KeyDown += (_, key, _) =>
        {
            if (key == Key.F1) _showDebug = !_showDebug;
        };
        Load();
    }

    private void OnUpdate(double dt)
    {
        int steps = _timestep.Advance(dt);
        for (int i = 0; i < steps; i++) FixedUpdate(_timestep.Step);
        _stats.Steps += steps;
        Update(dt);
    }

    private void OnRender(double dt)
    {
        _stats.Record(dt);
        Draw(dt);
    }

    private void OnGui(double dt)
    {
        if (!_showDebug) return;
        ImGui.Begin("Engine");
        ImGui.Text($"FPS: {_stats.Fps:F0}  ({_stats.FrameMs:F2} ms)");
        ImGui.Text($"Fixed steps: {_stats.Steps}  (step {_timestep.Step * 1000:F2} ms)");
        ImGui.Text($"Alpha: {_timestep.Alpha:F2}");
        ImGui.Text($"Window: {_window.Width}x{_window.Height}");
        bool vsync = _window.VSync;
        if (ImGui.Checkbox("VSync", ref vsync)) _window.VSync = vsync;
        ImGui.Text("F1: toggle debug UI");
        ImGui.End();
        DebugGui();
    }

    public void Dispose() => _window.Dispose();
}

internal sealed class FrameStats
{
    private double _accum;
    private int _frames;

    public double Fps { get; private set; }
    public double FrameMs { get; private set; }
    public long Steps { get; set; }

    public void Record(double dt)
    {
        _accum += dt;
        _frames++;
        if (_accum >= 0.5)
        {
            Fps = _frames / _accum;
            FrameMs = _accum / _frames * 1000;
            _accum = 0;
            _frames = 0;
        }
    }
}
