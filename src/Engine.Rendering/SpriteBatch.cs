using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>
/// Collects sprites between <see cref="Begin"/> and <see cref="End"/> and draws them with as few GPU draw calls
/// as possible: consecutive sprites that share a texture go out in one call. Sprites are drawn in submission
/// order, so sort them (e.g. by Z) before submitting.
/// </summary>
/// <remarks>
/// This file is the public API; <c>SpriteBatch.Gpu.cs</c> holds the OpenGL buffer setup and flushing.
/// </remarks>
public sealed partial class SpriteBatch : IDisposable
{
    /// <summary>Most sprites held before an automatic flush. Limited by 16-bit indices (4 vertices per sprite).</summary>
    public const int MaxSprites = 8192;

    private readonly SpriteVertex[] _vertices = new SpriteVertex[MaxSprites * SpriteQuad.VertexCount];

    private Matrix4x4 _viewProjection;
    private Texture2D? _currentTexture;
    private int _spriteCount;
    private bool _begun;
    private int _frameDrawCalls;
    private int _frameSprites;

    /// <summary>
    /// Totals across every <see cref="Begin"/>/<see cref="End"/> pass since <see cref="ResetStats"/> was last called. A frame
    /// usually has several passes (background tiles, sprites, foreground tiles), so call <see cref="ResetStats"/> once at the
    /// start of each frame and read this when drawing the debug UI.
    /// </summary>
    public RenderStats Stats => new(_frameDrawCalls, _frameSprites);

    /// <summary>Zeroes <see cref="Stats"/>. Call once per frame, before the first pass.</summary>
    public void ResetStats()
    {
        _frameDrawCalls = 0;
        _frameSprites = 0;
    }

    /// <summary>Starts a batch. Sets alpha blending and disables depth testing (2D sprites are ordered by submission).</summary>
    /// <param name="viewProjection">World-to-clip matrix, normally <see cref="Camera2D.GetViewProjection"/>.</param>
    /// <exception cref="InvalidOperationException">A batch is already in progress.</exception>
    public void Begin(in Matrix4x4 viewProjection)
    {
        if (_begun) throw new InvalidOperationException("Begin was called twice without End.");

        _begun = true;
        _viewProjection = viewProjection;
        PrepareState();
    }

    /// <summary>Queues one sprite. Flushes first if the texture changes or the buffer is full.</summary>
    /// <param name="texture">The texture to draw with.</param>
    /// <param name="draw">Placement and appearance of the sprite.</param>
    /// <exception cref="InvalidOperationException"><see cref="Begin"/> has not been called.</exception>
    public void Draw(Texture2D texture, in SpriteDraw draw)
    {
        if (!_begun) throw new InvalidOperationException("Draw was called before Begin.");

        if (!ReferenceEquals(texture, _currentTexture) || _spriteCount == MaxSprites)
        {
            Flush();
            _currentTexture = texture;
        }

        SpriteQuad.Write(draw, _vertices.AsSpan(_spriteCount * SpriteQuad.VertexCount, SpriteQuad.VertexCount));
        _spriteCount++;
        _frameSprites++;
    }

    /// <summary>Draws anything still queued and adds this pass to <see cref="Stats"/>.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Begin"/> has not been called.</exception>
    public void End()
    {
        if (!_begun) throw new InvalidOperationException("End was called before Begin.");

        Flush();
        _currentTexture = null;
        _begun = false;
    }
}
