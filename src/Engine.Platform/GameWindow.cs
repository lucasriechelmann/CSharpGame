using Engine.Core.Input;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Engine.Platform;

/// <summary>
/// Owns the OS window, the OpenGL context, the input wiring and the ImGui overlay.
/// Everything Silk.NET-specific lives here so higher layers never touch it directly.
/// </summary>
public sealed class GameWindow : IDisposable
{
    private readonly IWindow _window;
    private IInputContext? _silkInput;
    private ImGuiController? _imgui;

    /// <summary>Configures a window requesting an OpenGL 3.3 core context. It opens when <see cref="Run"/> is called.</summary>
    /// <param name="title">Window title.</param>
    /// <param name="width">Initial width in pixels.</param>
    /// <param name="height">Initial height in pixels.</param>
    /// <param name="vsync">Whether to synchronise presentation with the display refresh.</param>
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
        _window.FocusChanged += focused => { if (!focused) Input.ReleaseAll(); };
        _window.Closing += OnClosing;
    }

    /// <summary>The OpenGL API. Only valid once <see cref="Load"/> has been raised.</summary>
    public GL Gl { get; private set; } = null!;

    /// <summary>Keyboard and mouse state. Usable immediately; it is fed with events once the window has loaded.</summary>
    public InputState Input { get; } = new();

    /// <summary>Current window width in pixels.</summary>
    public int Width => _window.Size.X;

    /// <summary>Current window height in pixels.</summary>
    public int Height => _window.Size.Y;

    /// <summary>Whether presentation is synchronised with the display refresh. Can be changed at runtime.</summary>
    public bool VSync { get => _window.VSync; set => _window.VSync = value; }

    /// <summary>Raised once, after the window, GL context, input and ImGui are ready.</summary>
    public event Action? Load;

    /// <summary>Raised every frame before rendering; the argument is the frame time in seconds.</summary>
    public event Action<double>? Update;

    /// <summary>Raised every frame to draw the scene; the argument is the frame time in seconds.</summary>
    public event Action<double>? Render;

    /// <summary>Raised after the scene is drawn and the ImGui frame has begun; issue ImGui calls here.</summary>
    public event Action<double>? Gui;

    /// <summary>Raised when the framebuffer is resized, with the new width and height in pixels.</summary>
    public event Action<int, int>? Resized;

    /// <summary>
    /// Raised when the window is closing, <b>while the OpenGL context still exists</b>. This is the place to delete GPU
    /// resources; after <see cref="Run"/> returns the context is gone and GL calls fail.
    /// </summary>
    public event Action? Closing;

    /// <summary>Requests the window to close, ending <see cref="Run"/>.</summary>
    public void Close() => _window.Close();

    /// <summary>Opens the window and blocks, running the event loop until the window closes.</summary>
    public void Run() => _window.Run();

    private void OnLoad()
    {
        Gl = _window.CreateOpenGL();
        _silkInput = _window.CreateInput();
        _imgui = new ImGuiController(Gl, _window, _silkInput);
        _ = new SilkInputAdapter(
            _silkInput,
            Input,
            () => ImGui.GetIO().WantCaptureKeyboard,
            () => ImGui.GetIO().WantCaptureMouse);
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

    /// <summary>
    /// Runs while the context still exists: lets the game free its GPU resources first, then frees the ImGui renderer
    /// (which owns GL objects) and the input context.
    /// </summary>
    private void OnClosing()
    {
        Closing?.Invoke();

        _imgui?.Dispose();
        _imgui = null;
        _silkInput?.Dispose();
        _silkInput = null;
    }

    /// <summary>Releases the GL bindings and the window. GPU resources have already been freed when the window closed.</summary>
    public void Dispose()
    {
        Gl?.Dispose();
        _window.Dispose();
    }
}
