using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Runs the scene through an ordered list of <see cref="IPostEffect"/>s. Call <see cref="BeginScene"/> before drawing the frame
/// and <see cref="EndScene"/> after: while any effect is enabled the scene is drawn into an off-screen target, then each effect
/// reads one target and writes the other ("ping-pong") until the last effect writes to the window.
/// </summary>
/// <remarks>
/// Why ping-pong: a shader cannot read and write the same image in one pass, so two targets swap roles each effect. With no
/// effect enabled nothing is redirected, so unused post-processing costs nothing.
/// </remarks>
public sealed class PostProcessChain : IDisposable
{
    private readonly GL _gl;
    private readonly RenderTarget[] _targets;
    private readonly FullscreenQuad _quad;
    private readonly PostProcessContext _context;
    private readonly List<IPostEffect> _effects = new();
    private readonly List<IPostEffect> _active = new();
    private bool _sceneRedirected;

    /// <summary>Creates the chain with two targets of the window's size. Needs a current GL context.</summary>
    public PostProcessChain(GL gl, int width, int height)
    {
        _gl = gl;
        _quad = new FullscreenQuad(gl);
        // Only target 0 receives the scene, so only it needs a depth buffer (3D meshes are depth-tested while the scene is drawn).
        _targets = [new RenderTarget(gl, width, height, withDepth: true), new RenderTarget(gl, width, height)];
        _context = new PostProcessContext(gl, _quad, width, height);
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

    /// <summary>Appends an effect; the chain owns it and disposes it.</summary>
    /// <returns>The same effect, so the caller can keep a typed reference to tweak it.</returns>
    public T Add<T>(T effect) where T : IPostEffect
    {
        _effects.Add(effect);
        return effect;
    }

    /// <summary>Resizes the targets to the new window size. Call from the window's resize event.</summary>
    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return; // a minimised window reports 0x0; keep the old targets

        foreach (var target in _targets) target.Resize(width, height);
        _context.ScreenWidth = width;
        _context.ScreenHeight = height;
    }

    /// <summary>Directs the coming frame into the scene target when any effect is enabled; otherwise leaves the window bound.</summary>
    public void BeginScene()
    {
        _sceneRedirected = IsActive;
        if (_sceneRedirected) _targets[0].Bind();
    }

    /// <summary>Applies the enabled effects to the scene and presents the result to the window. A no-op when the scene was not redirected.</summary>
    public void EndScene()
    {
        if (!_sceneRedirected) return;

        _active.Clear();
        foreach (var effect in _effects)
            if (effect.Enabled) _active.Add(effect);

        int current = 0;
        for (int i = 0; i < _active.Count; i++)
        {
            bool last = i == _active.Count - 1;
            _active[i].Apply(_context, _targets[current].Texture, last ? null : _targets[1 - current]);
            current = 1 - current;
        }

        _gl.BindVertexArray(0);
        _sceneRedirected = false;
    }

    /// <summary>Disposes every effect, both targets and the full-screen triangle.</summary>
    public void Dispose()
    {
        foreach (var effect in _effects) effect.Dispose();
        foreach (var target in _targets) target.Dispose();
        _quad.Dispose();
    }
}
