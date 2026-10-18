using Silk.NET.OpenGL;

namespace Engine.Rendering;

// Scene settings: HDR colour, multisampling, pixel-perfect low-resolution rendering, and the final present to the window.
public sealed partial class PostProcessChain
{
    private const string PresentFragment = """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(texture(uTexture, vUv).rgb, 1.0);
        }
        """;

    /// <summary>
    /// Keeps the whole chain in 16-bit float colour instead of 8-bit, so lights and glows can go above 1 without clipping. Add a
    /// <see cref="ToneMapEffect"/> last to bring the result back to what a screen can show. Changing it recreates the targets.
    /// </summary>
    public bool Hdr
    {
        get => _hdr;
        set
        {
            if (_hdr == value) return;

            _hdr = value;
            _context.IsHdr = value;
            RebuildTargets();
        }
    }

    /// <summary>
    /// Multisampling of the scene target: 1 is off; 2, 4 or 8 draw each pixel several times at different positions and average, which
    /// smooths edges of rotated sprites and 3D meshes. Costs memory and fill rate. Changing it recreates the targets.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not 1, 2, 4, 8 or 16.</exception>
    public int Samples
    {
        get => _samples;
        set
        {
            new RenderTargetFormat(Samples: value).Validate();
            if (_samples == value) return;

            _samples = value;
            RebuildTargets();
        }
    }

    /// <summary>True while a low-resolution game image is being scaled into the window (see <see cref="SetPixelPerfect"/>).</summary>
    public bool IsPixelPerfect => _virtualWidth > 0;

    /// <summary>Width of the game image in pixel-perfect mode, else 0.</summary>
    public int VirtualWidth => _virtualWidth;

    /// <summary>Height of the game image in pixel-perfect mode, else 0.</summary>
    public int VirtualHeight => _virtualHeight;

    /// <summary>Where the game image sits in the window (valid in pixel-perfect mode); use it to convert mouse positions.</summary>
    public PixelPerfectLayout PixelPerfectLayout =>
        IsPixelPerfect ? PixelPerfectLayout.Compute(_virtualWidth, _virtualHeight, _windowWidth, _windowHeight) : throw new InvalidOperationException("The chain is not in pixel-perfect mode.");

    /// <summary>
    /// Draws the whole scene at a small fixed resolution (say 480x270) and scales it up to the window by a whole number with sharp pixels
    /// and black bars, so pixel art stays crisp at any window size. Effects run at the small resolution too. The game should set its
    /// camera's <c>ViewportSize</c> to the same size and turn on <c>Camera2D.PixelSnap</c>.
    /// </summary>
    /// <param name="width">Game image width in pixels.</param>
    /// <param name="height">Game image height in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size is not positive.</exception>
    public void SetPixelPerfect(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), $"The game image must have a positive size, got {width}x{height}.");
        if (width == _virtualWidth && height == _virtualHeight) return;

        _virtualWidth = width;
        _virtualHeight = height;
        RebuildTargets();
    }

    /// <summary>Goes back to drawing at the window's resolution.</summary>
    public void ClearPixelPerfect()
    {
        if (!IsPixelPerfect) return;

        _virtualWidth = _virtualHeight = 0;
        RebuildTargets();
    }

    /// <summary>
    /// The finished image when <see cref="UseOffscreenOutput"/> is on (an editor shows it inside a panel), else null. It has the size of the
    /// last <see cref="Resize"/>, and its rows run bottom to top like any GL texture.
    /// </summary>
    public Texture2D? OutputTexture => _offscreen?.Texture;

    /// <summary>
    /// True sends the finished image into a texture (<see cref="OutputTexture"/>) instead of the window, so a tool can show the game inside
    /// one of its own panels; the window is then free for the tool's interface. The chain keeps that texture the size given to <see cref="Resize"/>.
    /// </summary>
    /// <param name="on">Whether to draw to the texture instead of the window.</param>
    public void UseOffscreenOutput(bool on)
    {
        if (on == (_offscreen != null)) return;

        if (on)
        {
            _offscreen = new RenderTarget(_gl, _windowWidth, _windowHeight, RenderTargetFormat.Default);
        }
        else
        {
            _offscreen!.Dispose();
            _offscreen = null;
        }
    }

    /// <summary>The scene must be drawn off-screen even with no effect enabled: HDR, multisampling and the low-resolution image all need a target.</summary>
    private bool NeedsSceneTarget => _hdr || _samples > 1 || IsPixelPerfect || _offscreen != null;

    private void RebuildTargets()
    {
        DisposeTargets();

        int width = IsPixelPerfect ? _virtualWidth : _windowWidth;
        int height = IsPixelPerfect ? _virtualHeight : _windowHeight;
        // Nearest filtering matters only for the image that gets scaled up; the scene target is also the first effect's input.
        var filter = IsPixelPerfect ? TextureFilter.Nearest : TextureFilter.Linear;

        // Only the scene target receives the scene, so only it needs depth (3D meshes are depth-tested while the scene is drawn) and samples.
        _scene = new RenderTarget(_gl, width, height, new RenderTargetFormat(Hdr: _hdr, Depth: true, Samples: _samples, Filter: filter));
        _pingPong =
        [
            new RenderTarget(_gl, width, height, new RenderTargetFormat(Hdr: _hdr, Filter: filter)),
            new RenderTarget(_gl, width, height, new RenderTargetFormat(Hdr: _hdr, Filter: filter)),
        ];

        _context.ScreenWidth = width;
        _context.ScreenHeight = height;
        _context.IsHdr = _hdr;
    }

    private void DisposeTargets()
    {
        _scene?.Dispose();
        _scene = null;
        foreach (var target in _pingPong) target.Dispose();
        _pingPong = [];
    }

    /// <summary>Draws a finished image to the window: stretched over it normally, or scaled by a whole number and centred in pixel-perfect mode.</summary>
    private void Present(Texture2D image)
    {
        if (_offscreen != null) _offscreen.Bind();
        else RenderTarget.BindScreen(_gl, _windowWidth, _windowHeight);
        _gl.Disable(EnableCap.Blend);
        _gl.Disable(EnableCap.DepthTest);

        if (IsPixelPerfect)
        {
            _gl.ClearColor(0f, 0f, 0f, 1f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            var layout = PixelPerfectLayout;
            // GL's origin is bottom-left; the layout is centred, so the top and bottom margins are equal and no flip is needed.
            _gl.Viewport(layout.X, layout.Y, (uint)layout.Width, (uint)layout.Height);
        }

        _present.Use();
        image.Bind(0);
        _present.SetUniform("uTexture", 0);
        _quad.Draw();
    }
}
