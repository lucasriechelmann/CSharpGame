using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Engine.Platform;

/// <summary>
/// Owns the OS window, the OpenGL context, the input context and the ImGui overlay.
/// Everything Silk.NET-specific lives here so higher layers never touch it directly.
/// </summary>
public sealed class GameWindow : IDisposable
{
    private readonly IWindow _window;
    private ImGuiController? _imgui;

    public GameWindow(string title, int width, int height, bool vsync = true)
    {
        var options = WindowOptions.Default with
        {
            Title = title,
            Size = new Vector2D<int>(width, height),
            VSync = vsync,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3)),
        };
        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Update += dt => Update?.Invoke(dt);
        _window.Render += OnRender;
        _window.FramebufferResize += size => { Gl?.Viewport(size); Resized?.Invoke(size.X, size.Y); };
        _window.Closing += () => Closing?.Invoke();
    }

    public GL Gl { get; private set; } = null!;
    public IInputContext Input { get; private set; } = null!;
    public int Width => _window.Size.X;
    public int Height => _window.Size.Y;
    public bool VSync { get => _window.VSync; set => _window.VSync = value; }

    public event Action? Load;
    public event Action<double>? Update;
    public event Action<double>? Render;
    /// <summary>Raised after the scene is drawn and the ImGui frame has begun; issue ImGui calls here.</summary>
    public event Action<double>? Gui;
    public event Action<int, int>? Resized;
    public event Action? Closing;

    public void Close() => _window.Close();

    public void Run() => _window.Run();

    private void OnLoad()
    {
        Gl = _window.CreateOpenGL();
        Input = _window.CreateInput();
        _imgui = new ImGuiController(Gl, _window, Input);
        Gl.Viewport(_window.FramebufferSize);
        Load?.Invoke();
    }

    private void OnRender(double dt)
    {
        Render?.Invoke(dt);
        _imgui!.Update((float)dt);
        Gui?.Invoke(dt);
        _imgui.Render();
    }

    public void Dispose()
    {
        _imgui?.Dispose();
        Input?.Dispose();
        Gl?.Dispose();
        _window.Dispose();
    }
}
