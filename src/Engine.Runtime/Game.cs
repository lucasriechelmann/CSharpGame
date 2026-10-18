using Engine.Animation;
using Engine.Audio;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Diagnostics;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Platform;
using Engine.Runtime.Inspector;
using Engine.Rendering;
using Engine.Scenes;
using Engine.Tilemaps;
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
    private readonly EntityInspector _inspector;
    private readonly ProfilerPanel _profilerPanel;
    private PostEffectsPanel _postEffectsPanel = null!;
    private int _postProcessSection;
    private readonly int _frameSection;
    private readonly int _updateSection;
    private readonly int _fixedUpdateSection;
    private readonly int _drawSection;
    private readonly int _debugGuiSection;
    private long _frameStart;

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
        Scenes = new SceneSerializer(World, Assets, Components);
        _overlay = new DebugOverlay(_window, _timestep, _stats, World, Assets, () => Audio);
        _inspector = new EntityInspector(World, Components);
        _profilerPanel = new ProfilerPanel(Profiler);
        FixedSystems.Profile(Profiler, "Fixed");
        FrameSystems.Profile(Profiler, "Frame");
        RenderSystems.Profile(Profiler, "Render");
        _frameSection = Profiler.GetSection("CPU frame");
        _updateSection = Profiler.GetSection("Game.Update");
        _fixedUpdateSection = Profiler.GetSection("Game.FixedUpdate");
        _drawSection = Profiler.GetSection("Game.Draw");
        _debugGuiSection = Profiler.GetSection("Debug UI");
        _window.Load += OnLoad;
        _window.Closing += OnClosing;
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

    /// <summary>
    /// The components that may appear in scene and prefab files. The engine's own are registered already; register your game's
    /// components (<c>Components.Register&lt;Patrol&gt;("Patrol")</c>) in the constructor, before loading any scene.
    /// </summary>
    public ComponentRegistry Components { get; } = EngineComponents.RegisterAll(new ComponentRegistry());

    /// <summary>
    /// Saves the world to JSON and loads scenes and prefabs into it. Prefab, tile layer, tile set and animation loaders are
    /// registered automatically once the window is ready, so scenes can refer to those files by name from <see cref="Load"/> onwards.
    /// </summary>
    public SceneSerializer Scenes { get; }

    /// <summary>
    /// Per-section frame timing. Every system in <see cref="FixedSystems"/>, <see cref="FrameSystems"/> and <see cref="RenderSystems"/>
    /// and each game hook is measured automatically; add your own sections with <see cref="Profiler.GetSection"/>.
    /// </summary>
    public Profiler Profiler { get; } = new();

    /// <summary>
    /// Plays sounds. Silent until the window loads; from <see cref="Load"/> onwards it is an OpenAL device, or a silent stand-in
    /// (whose <see cref="IAudioDevice.Description"/> says why) when the machine has no usable audio output. Load clips with
    /// <c>Assets.Load&lt;AudioClip&gt;("sounds/jump.wav")</c>.
    /// </summary>
    public IAudioDevice Audio { get; private set; } = new NullAudioDevice("starts when the window loads");

    /// <summary>
    /// Full-screen effects (lighting, bloom, vignette, colour grade) applied to the finished scene. Available from <see cref="Load"/>
    /// onwards: add effects there, e.g. <c>PostProcess.Add(new BloomEffect(Gl))</c>. While no effect is enabled the scene draws
    /// straight to the window and this costs nothing.
    /// </summary>
    public PostProcessChain PostProcess { get; private set; } = null!;

    /// <summary>
    /// The render interpolation factor, refreshed every frame before <see cref="Draw"/>. Pass it to the render systems
    /// (<c>new SpriteRenderSystem(..., RenderClock)</c>) and give moving entities a <c>PreviousTransform</c>.
    /// </summary>
    public RenderClock RenderClock { get; } = new();

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

    /// <summary>Called once when the window closes, while the GL context still exists. Dispose GPU resources created in <see cref="Load"/> here.</summary>
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

    /// <summary>
    /// The game's own on-screen UI (score, lives, messages) as ImGui calls. Unlike <see cref="DebugGui"/> it is drawn every frame,
    /// even when F1 has hidden the debug UI, because players need it. Drawn after post-processing, so effects never dim it.
    /// </summary>
    protected virtual void Hud() { }

    /// <summary>Opens the window and runs the game loop until the window closes.</summary>
    public void Run() => _window.Run();

    /// <summary>
    /// Releases the window. GPU resources (<see cref="Unload"/> and every asset) are freed earlier, when the window closes,
    /// because OpenGL calls only work while the context exists.
    /// </summary>
    public void Dispose() => _window.Dispose();

    /// <summary>Frees the game's GPU resources and every loaded asset while the OpenGL context still exists.</summary>
    private void OnClosing()
    {
        Unload();
        Audio.Dispose();
        PostProcess.Dispose();
        Assets.Dispose();
    }

    private void OnLoad()
    {
        Assets.Register(new TextureLoader(Gl));
        Assets.Register(new TileSetLoader(Assets));
        Assets.Register(new TileLayerLoader());
        Assets.Register(new AnimationStateMachineLoader());
        Assets.Register(new PrefabLoader());
        Assets.Register(new AudioClipLoader());
        PostProcess = new PostProcessChain(Gl, _window.Width, _window.Height);
        _postEffectsPanel = new PostEffectsPanel(PostProcess);
        _postProcessSection = Profiler.GetSection("Post process");
        _window.Resized += PostProcess.Resize;
        Audio = OpenAlAudioDevice.TryCreate(out var device, out string reason) ? device! : new NullAudioDevice(reason);
        Load();
    }

    /// <summary>
    /// One frame of logic: per-frame code first (so input is read once and latched into components), then the
    /// fixed simulation steps that consume it, then input edges are cleared.
    /// </summary>
    private void OnUpdate(double dt)
    {
        Profiler.BeginFrame();
        _frameStart = Profiler.Begin();
        if (Input.WasPressed(KeyCode.F1)) _overlay.Toggle();
        Audio.Update();

        FrameSystems.Run(World, dt);
        long start = Profiler.Begin();
        Update(dt);
        Profiler.End(_updateSection, start);

        int steps = _timestep.Advance(dt);
        for (int i = 0; i < steps; i++)
        {
            FixedSystems.Run(World, _timestep.Step);
            start = Profiler.Begin();
            FixedUpdate(_timestep.Step);
            Profiler.End(_fixedUpdateSection, start);
        }

        _stats.RecordFixedSteps(steps);
        Input.EndFrame();
    }

    private void OnRender(double dt)
    {
        _stats.RecordFrame(dt);
        RenderClock.Alpha = (float)_timestep.Alpha;
        PostProcess.BeginScene();
        long start = Profiler.Begin();
        Draw(dt);
        Profiler.End(_drawSection, start);
        RenderSystems.Run(World, dt);
        start = Profiler.Begin();
        PostProcess.EndScene();
        Profiler.End(_postProcessSection, start);
    }

    private void OnGui(double dt)
    {
        long start = Profiler.Begin();
        Hud();
        if (_overlay.Visible)
        {
            _overlay.Draw();
            if (_overlay.ShowInspector) _inspector.Draw();
            if (_overlay.ShowProfiler) _profilerPanel.Draw();
            if (_overlay.ShowPostEffects) _postEffectsPanel.Draw();
            DebugGui();
        }

        Profiler.End(_debugGuiSection, start);
        // The GUI callback is the last thing in a frame, so this closes the whole-frame measurement even when the overlay is hidden.
        Profiler.End(_frameSection, _frameStart);
    }
}
