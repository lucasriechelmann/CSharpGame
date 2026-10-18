using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Runs the scene through an ordered list of <see cref="IPostEffect"/>s. Call <see cref="BeginScene"/> before drawing the frame
/// and <see cref="EndScene"/> after: while any effect is enabled the scene is drawn into an off-screen target, then each effect
/// reads one target and writes the other ("ping-pong") until the last effect writes to the window.
/// </summary>
/// <remarks>
/// Why ping-pong: a shader cannot read and write the same image in one pass, so two targets swap roles each effect. With no
/// effect enabled and no scene setting on, nothing is redirected, so unused post-processing costs nothing. Three scene settings
/// also redirect the scene: <see cref="Hdr"/> (16-bit float colour for the whole chain), <see cref="Samples"/> (multisampling that
/// smooths 3D and rotated-sprite edges) and <see cref="SetPixelPerfect"/> (a low-resolution image scaled up by whole numbers).
/// </remarks>
public sealed partial class PostProcessChain : IDisposable
{
    private readonly GL _gl;
    private readonly FullscreenQuad _quad;
    private readonly PostProcessContext _context;
    private readonly Shader _present;
    private readonly List<IPostEffect> _effects = new();
    private readonly List<IPostEffect> _active = new();
    private RenderTarget? _scene;
    private RenderTarget[] _pingPong = [];
    private RenderTarget? _offscreen;
    private int _windowWidth;
    private int _windowHeight;
    private bool _hdr;
    private int _samples = 1;
    private int _virtualWidth;
    private int _virtualHeight;
    private bool _sceneRedirected;

    /// <summary>Creates the chain with two targets of the window's size. Needs a current GL context.</summary>
    /// <param name="gl">The GL context that will own the targets and effects.</param>
    /// <param name="width">Window width in pixels.</param>
    /// <param name="height">Window height in pixels.</param>
    public PostProcessChain(GL gl, int width, int height)
    {
        _gl = gl;
        _quad = new FullscreenQuad(gl);
        _present = new Shader(gl, FullscreenQuad.VertexSource, PresentFragment);
        _windowWidth = width;
        _windowHeight = height;
        _context = new PostProcessContext(gl, _quad, width, height);
        RebuildTargets();
    }

    /// <summary>The effects in execution order.</summary>
    public IReadOnlyList<IPostEffect> Effects => _effects;

    /// <summary>True when at least one effect is enabled, so the scene needs to be redirected into a target.</summary>
    public bool IsActive
    {
        get
        {
            foreach (var effect in _effects)
                if (effect.Enabled) return true;
            return false;
        }
    }

    /// <summary>Appends an effect to the end of the chain and returns it so settings can be set in the same statement.</summary>
    public T Add<T>(T effect) where T : IPostEffect
    {
        _effects.Add(effect);
        return effect;
    }

    /// <summary>Tells the chain the window changed size. Ignored when the size is zero (a minimised window).</summary>
    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return; // a minimised window reports 0x0; keep the old targets

        _windowWidth = width;
        _windowHeight = height;
        _offscreen?.Resize(width, height);
        if (IsPixelPerfect) return; // the targets keep their game resolution; only where they are drawn changes

        _scene!.Resize(width, height);
        foreach (var target in _pingPong) target.Resize(width, height);
        _context.ScreenWidth = width;
        _context.ScreenHeight = height;
    }

    /// <summary>Starts the frame: when anything needs the scene off-screen, redirects drawing into the scene target.</summary>
    public void BeginScene()
    {
        _sceneRedirected = IsActive || NeedsSceneTarget;
        if (_sceneRedirected) _scene!.Bind();
    }

    /// <summary>Ends the frame: resolves multisampling, runs the active effects and presents the result to the window.</summary>
    public void EndScene()
    {
        if (!_sceneRedirected) return;

        _scene!.Resolve();

        _active.Clear();
        foreach (var effect in _effects)
            if (effect.Enabled) _active.Add(effect);

        // The first effect reads the scene; every other reads the previous effect's output. Each writes to the ping-pong target that is not
        // its own source, so nothing is read and written at once, and none ever draws into the (possibly multisampled) scene target.
        var source = _scene.Texture;
        int next = 0;
        for (int i = 0; i < _active.Count; i++)
        {
            // The last effect draws straight to the window, unless the image still has to be scaled up into it.
            bool toWindow = i == _active.Count - 1 && !IsPixelPerfect;
            var destination = toWindow ? _offscreen : _pingPong[next];
            _active[i].Apply(_context, source, destination);
            if (destination == null) continue;

            source = destination.Texture;
            next = 1 - next;
        }

        if (_active.Count == 0 || IsPixelPerfect) Present(source);

        if (_offscreen != null)
        {
            // The finished image is in the texture; hand the window back to whoever draws on it next (the editor's interface) with a clean slate.
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.ClearColor(0.07f, 0.07f, 0.09f, 1f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        _gl.BindVertexArray(0);
        _sceneRedirected = false;
    }

    /// <summary>Frees the effects, targets and shaders.</summary>
    public void Dispose()
    {
        foreach (var effect in _effects) effect.Dispose();
        DisposeTargets();
        _offscreen?.Dispose();
        _present.Dispose();
        _quad.Dispose();
    }
}
