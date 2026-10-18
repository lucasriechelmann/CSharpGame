using Engine.Animation;
using Engine.Audio;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Diagnostics;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Core.Logging;
using Engine.Platform;
using Engine.Runtime.Editor;
using Engine.Runtime.Inspector;
using Engine.Rendering;
using Engine.Rendering3D;
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
    private readonly LogPanel _logPanel;
    private readonly SceneEditor _editor;
    private AssetHotReloader? _hotReloader;
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
        var logHistory = new MemoryLogSink();
        Log.AddSink(new ConsoleLogSink()).AddSink(logHistory);
        _logPanel = new LogPanel(logHistory, Log);
        _editor = new SceneEditor(World, Assets, _inspector, Log, new EditorHooks(() => SceneCamera, () => SceneCamera3D, () => ScenePath, path => Scenes.SaveToFile(path), LoadSceneFromEditor, EditableAssetPath, path => _hotReloader?.Ignore(path),
            () => Scenes.Save(), RestoreSceneFromEditor, PlayStarted, PlayStopped,
            e => World.TryGet<PrefabLink>(e, out var link) && !string.IsNullOrEmpty(link.Prefab) ? link.Prefab : null, Scenes.SaveAsPrefab, Scenes.RevertToPrefab));
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
    /// Where the engine and the game write events ("Audio: OpenAL Soft ready", "Game: quick-saved 37 entities"). Entries go to the
    /// console and to the "Log" debug window; add your own <see cref="ILogSink"/> for anything else. Debug-level messages are
    /// dropped until <see cref="Logger.MinimumLevel"/> is lowered (the Log window has a selector).
    /// </summary>
    public Logger Log { get; } = new();

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

    /// <summary>
    /// While true the simulation is frozen: <see cref="FrameSystems"/> and <see cref="FixedSystems"/> do not run and the fixed
    /// timestep stops accumulating time, so nothing moves or animates and no time is "owed" when you resume. <see cref="Update"/>,
    /// drawing and the GUI keep running, so a game can read input and show a menu. Set it from <see cref="Update"/> or <see cref="Hud"/>.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// True while the scene editor (F2) is on and the game is not playing inside it. The simulation is frozen exactly as with
    /// <see cref="IsPaused"/>, so game code that advances its own clocks or reads gameplay input from <see cref="Update"/> should skip
    /// itself while this is true. It is false again between the editor's Play and Stop, when the game runs for real.
    /// </summary>
    public bool IsEditing => _editor.Editing;

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

    /// <summary>
    /// The camera that draws the scene. Return it so the editor can pick entities under the mouse, draw the gizmo and pan and zoom
    /// the view; with the default (null) the editor still has its panels but no scene view. A game that follows something with this
    /// camera should stop while <see cref="IsEditing"/>.
    /// </summary>
    protected virtual Camera2D? SceneCamera => null;

    /// <summary>
    /// The 3D camera that draws the scene. Return it and the editor (F2) gives the scene an orbit camera (middle-drag orbits, Shift +
    /// middle-drag pans, wheel zooms, F frames the selection), click-to-select by mesh bounds and a transform gizmo. The editor writes
    /// this camera every frame while <see cref="IsEditing"/>, so a game that also moves it (a fly camera) must stop while editing and
    /// take the camera's view back when editing ends (<c>FlyCamera.CopyFrom</c>). When both cameras are returned the 3D one wins.
    /// </summary>
    protected virtual Camera3D? SceneCamera3D => null;

    /// <summary>The scene file the editor saves to and reloads from, as a full path; null disables those menu items.</summary>
    protected virtual string? ScenePath => null;

    /// <summary>
    /// The file the editor writes an edited asset (a painted tile layer) to, given its asset name such as <c>levels/ground.layer.json</c>.
    /// The default is the file the asset was loaded from, in the <c>Assets</c> folder next to the executable; override it to point at
    /// your project source assets so an edit is a change you can commit.
    /// </summary>
    /// <param name="assetName">The asset name, relative to the asset root.</param>
    protected virtual string EditableAssetPath(string assetName) => Path.Combine(Assets.RootDirectory, assetName);

    /// <summary>
    /// A folder to watch for edited asset files, laid out like <see cref="Assets"/> (usually your project's source <c>Assets</c> folder,
    /// which is not the copy next to the executable). While the game runs, a changed texture, tile layer, tile set, prefab, animation or
    /// sound that is already loaded is reloaded in place (see <see cref="AssetHotReloader"/> for what that cannot update). Null, the
    /// default, turns hot-reload off.
    /// </summary>
    protected virtual string? HotReloadRoot => null;

    /// <summary>
    /// Called after the editor replaced the world with a scene file (Reload). Re-find whatever you cached from the old world
    /// (the hero entity, counts) here, as you do after a quick-load.
    /// </summary>
    protected virtual void SceneReloaded() { }

    /// <summary>
    /// Called when the editor's Play button is pressed, after the world was saved for Stop. Start a fresh run from the edited world here
    /// (reset the score and clocks, snap the camera, rebuild anything derived from level data such as collision).
    /// </summary>
    protected virtual void PlayStarted() { }

    /// <summary>
    /// Called when the editor's Stop button is pressed, after the world was restored to what it was at Play (<see cref="SceneReloaded"/>
    /// has already run). Throw away the state of the run that was played.
    /// </summary>
    protected virtual void PlayStopped() { }

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

    /// <summary>Asks the window to close; the game loop ends after the current frame and <see cref="Unload"/> runs as usual.</summary>
    public void Quit() => _window.Close();

    /// <summary>Opens the window and runs the game loop until the window closes.</summary>
    public void Run() => _window.Run();

    /// <summary>
    /// Releases the window. GPU resources (<see cref="Unload"/> and every asset) are freed earlier, when the window closes,
    /// because OpenGL calls only work while the context exists.
    /// </summary>
    public void Dispose() => _window.Dispose();

    /// <summary>Frees the game's GPU resources and every loaded asset while the OpenGL context still exists.</summary>
    /// <summary>The editor's Reload: replaces the world with the scene file, then lets the game re-establish what it cached.</summary>
    private void LoadSceneFromEditor(string path)
    {
        World.Clear();
        Scenes.LoadFile(path);
        SceneReloaded();
    }

    /// <summary>The editor's Stop: replaces the world with the snapshot taken at Play, then lets the game re-establish what it cached.</summary>
    private void RestoreSceneFromEditor(string json)
    {
        World.Clear();
        Scenes.Load(json);
        SceneReloaded();
    }

    private void OnClosing()
    {
        Unload();
        _hotReloader?.Dispose();
        Audio.Dispose();
        PostProcess.Dispose();
        Assets.Dispose();
    }

    private void OnLoad()
    {
        Assets.Register(new TextureLoader(Gl));
        Assets.Register(new TileSetLoader(Assets));
        Assets.Register(new MaterialLoader(Assets));
        Assets.Register(new ModelLoader(Gl));
        Assets.Register(new TileLayerLoader());
        Assets.Register(new AnimationStateMachineLoader());
        Assets.Register(new PrefabLoader());
        Assets.Register(new AudioClipLoader());
        PostProcess = new PostProcessChain(Gl, _window.Width, _window.Height);
        _postEffectsPanel = new PostEffectsPanel(PostProcess);
        _postProcessSection = Profiler.GetSection("Post process");
        _window.Resized += PostProcess.Resize;
        bool audioReady = OpenAlAudioDevice.TryCreate(out var device, out string reason);
        Audio = audioReady ? device! : new NullAudioDevice(reason);
        if (audioReady) Log.Info("Audio", Audio.Description);
        else Log.Warning("Audio", $"No sound: {reason}");
        Log.Info("Engine", $"Window {_window.Width}x{_window.Height}, fixed step {_timestep.Step * 1000:F2} ms");
        Load();
        StartHotReload();
    }

    private void StartHotReload()
    {
        string? root = HotReloadRoot;
        if (root == null) return;
        if (!Directory.Exists(root))
        {
            Log.Warning("Assets", $"Hot-reload is off: '{root}' does not exist");
            return;
        }

        _hotReloader = new AssetHotReloader(Assets, root, Log);
        _hotReloader.Start();
        Log.Info("Assets", $"Hot-reload watching {root}");
    }

    /// <summary>
    /// One frame of logic: per-frame code first (so input is read once and latched into components), then the
    /// fixed simulation steps that consume it, then input edges are cleared.
    /// </summary>
    private void OnUpdate(double dt)
    {
        Profiler.BeginFrame();
        _frameStart = Profiler.Begin();
        _hotReloader?.Poll();
        if (Input.WasPressed(KeyCode.F1)) _overlay.Toggle();
        if (Input.WasPressed(KeyCode.F2)) _editor.Toggle();
        Audio.Update();

        bool frozen = IsPaused || IsEditing;
        if (!frozen) FrameSystems.Run(World, dt);
        long start = Profiler.Begin();
        Update(dt);
        Profiler.End(_updateSection, start);

        int steps = frozen ? 0 : _timestep.Advance(dt);
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
        // While frozen nothing moves, so draw the simulated state itself: a stale fraction would leave moved or paused entities part-way back.
        RenderClock.Alpha = IsPaused || IsEditing ? 1f : (float)_timestep.Alpha;
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
            if (_overlay.ShowInspector && !_editor.Active) _inspector.Draw(); // the editor has its own hierarchy and inspector windows
            if (_overlay.ShowProfiler) _profilerPanel.Draw();
            if (_overlay.ShowPostEffects) _postEffectsPanel.Draw();
            if (_overlay.ShowLog) _logPanel.Draw();
            DebugGui();
        }

        if (_editor.Active) _editor.Draw();

        Profiler.End(_debugGuiSection, start);
        // The GUI callback is the last thing in a frame, so this closes the whole-frame measurement even when the overlay is hidden.
        Profiler.End(_frameSection, _frameStart);
    }
}
