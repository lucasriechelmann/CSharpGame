using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Tilemaps;

/// <summary>
/// Draws every <see cref="TileLayerRenderer"/>, visiting only the chunks the camera can see. Tile layers are drawn
/// in their own pass: register one instance with <c>foreground: false</c> before the sprite system (background and ground)
/// and one with <c>foreground: true</c> after it (decoration in front of characters). Layers within a pass are ordered by Z.
/// </summary>
/// <remarks>
/// Each visible chunk is drawn from a <b>cached mesh</b> (<see cref="TileMeshCache"/>): its static tiles are built into GPU buffers once and redrawn with one call, placed
/// and tinted through uniforms, so panning, parallax and fading cost nothing per tile. Animated tiles change picture, so they are left out of the mesh and drawn
/// through the sprite batch every frame. The system owns GPU buffers; the game frees them (it disposes every system that is <see cref="IDisposable"/>) when it closes.
/// </remarks>
public sealed class TileLayerRenderSystem : ISystem, IDisposable
{
    private static readonly Comparison<TileLayerDraw> ByDepth = (a, b) => a.Origin.Z.CompareTo(b.Origin.Z);

    private readonly SpriteBatch _batch;
    private readonly Camera2D _camera;
    private readonly AssetManager _assets;
    private readonly bool _foreground;
    private readonly List<TileLayerDraw> _layers = new();
    private readonly List<AnimatedCell> _animated = new();
    private readonly EntityAction<Transform, TileLayerRenderer> _collect;
    private readonly TileMeshCache _cache;
    private readonly Engine.Rendering.Shader _meshShader;
    private double _elapsed;

    /// <summary>Creates the system.</summary>
    /// <param name="batch">The batch animated tiles are drawn with; its GL context also draws the cached meshes.</param>
    /// <param name="camera">The camera that defines what is visible.</param>
    /// <param name="assets">Resolves layer, tile set and texture handles.</param>
    /// <param name="foreground">True draws only layers marked foreground, false only the others.</param>
    public TileLayerRenderSystem(SpriteBatch batch, Camera2D camera, AssetManager assets, bool foreground = false)
    {
        _batch = batch;
        _camera = camera;
        _assets = assets;
        _foreground = foreground;
        _collect = Collect; // Created once so the per-frame query does not allocate a delegate.
        _cache = new TileMeshCache(batch.Gl);
        _meshShader = new Engine.Rendering.Shader(batch.Gl, TileMeshShaderSource.Vertex, TileMeshShaderSource.Fragment);
    }

    /// <summary>How many chunk meshes are cached right now (shown by tools).</summary>
    public int CachedChunks => _cache.Count;

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _elapsed += dt; // the animation clock: animated tiles advance in step wherever they are drawn
        _cache.BeginFrame();
        _layers.Clear();
        world.Query(_collect);
        if (_layers.Count == 0) return;

        _layers.Sort(ByDepth);
        _camera.GetVisibleBounds(out var visibleMin, out var visibleMax);
        var viewProjection = _camera.GetViewProjection();

        foreach (var layer in _layers)
        {
            DrawLayerMeshes(layer, visibleMin, visibleMax, viewProjection);
            DrawAnimatedTiles(layer, viewProjection);
        }
    }

    /// <summary>Frees the cached meshes and the mesh shader.</summary>
    public void Dispose()
    {
        _cache.Dispose();
        _meshShader.Dispose();
    }

    private void Collect(Entity entity, ref Transform transform, ref TileLayerRenderer renderer)
    {
        if (renderer.Foreground != _foreground || !renderer.Layer.IsValid || !renderer.TileSet.IsValid) return;

        var tileSet = _assets.Get(renderer.TileSet);
        _layers.Add(new TileLayerDraw
        {
            Origin = SnapKeepingDepth(ParallaxOrigin(transform.Position, _camera.ViewPosition, renderer.Parallax)),
            Layer = _assets.Get(renderer.Layer),
            TileSet = tileSet,
            Texture = _assets.Get(tileSet.Texture),
            Tint = renderer.Tint,
        });
    }

    /// <summary>Draws the cached mesh of every visible chunk, and collects those chunks' animated tiles for <see cref="DrawAnimatedTiles"/>.</summary>
    private void DrawLayerMeshes(in TileLayerDraw draw, Vector2 visibleMin, Vector2 visibleMax, in Matrix4x4 viewProjection)
    {
        var layer = draw.Layer;
        _animated.Clear();

        // Visible cell range, in layer space; whole chunks are drawn, so the range only chooses which chunks.
        int minChunkX = TileLayer.ChunkIndex(layer.CellIndex(visibleMin.X - draw.Origin.X));
        int maxChunkX = TileLayer.ChunkIndex(layer.CellIndex(visibleMax.X - draw.Origin.X));
        int minChunkY = TileLayer.ChunkIndex(layer.CellIndex(visibleMin.Y - draw.Origin.Y));
        int maxChunkY = TileLayer.ChunkIndex(layer.CellIndex(visibleMax.Y - draw.Origin.Y));

        var gl = _batch.Gl;
        bool prepared = false;
        for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
        {
            for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
            {
                if (!layer.TryGetChunk(chunkX, chunkY, out var tiles, out int version)) continue;

                var entry = _cache.Get(layer, chunkX, chunkY, tiles, version, draw.TileSet, draw.Texture);
                _animated.AddRange(entry.Animated);
                if (entry.Mesh is null) continue;

                if (!prepared)
                {
                    PrepareMeshState(gl, draw, viewProjection);
                    prepared = true;
                }

                entry.Mesh.Draw();
                _batch.CountExternal(1, entry.Mesh.QuadCount);
            }
        }

        if (prepared) gl.BindVertexArray(0);
    }

    private void PrepareMeshState(GL gl, in TileLayerDraw draw, in Matrix4x4 viewProjection)
    {
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.Disable(EnableCap.DepthTest);
        gl.Disable(EnableCap.CullFace);

        _meshShader.Use();
        _meshShader.SetUniform("uViewProjection", viewProjection);
        _meshShader.SetUniform("uOffset", draw.Origin);
        _meshShader.SetUniform("uTint", new Vector4(draw.Tint.R, draw.Tint.G, draw.Tint.B, draw.Tint.A));
        _meshShader.SetUniform("uTexture", 0);
        draw.Texture.Bind(0);
    }

    /// <summary>Draws the animated tiles collected for a layer through the sprite batch, showing each one's current frame.</summary>
    private void DrawAnimatedTiles(in TileLayerDraw draw, in Matrix4x4 viewProjection)
    {
        if (_animated.Count == 0) return;

        float tileSize = draw.Layer.TileSize;
        _batch.Begin(viewProjection);
        foreach (var cell in _animated)
        {
            int shown = draw.TileSet.ResolveFrame(cell.Raw, _elapsed); // the current frame, flip flags kept
            _batch.Draw(draw.Texture, new SpriteDraw
            {
                Position = new Vector3(
                    draw.Origin.X + (cell.CellX + 0.5f) * tileSize,
                    draw.Origin.Y + (cell.CellY + 0.5f) * tileSize,
                    draw.Origin.Z),
                Size = new Vector2(tileSize, tileSize),
                Pivot = new Vector2(0.5f, 0.5f),
                UvRect = draw.TileSet.GetSourceRect(shown).Inset(TileChunkMeshBuilder.AtlasInsetPixels).ToUv(draw.Texture.Width, draw.Texture.Height),
                Flip = TileChunkMeshBuilder.FlipOf(shown),
                Color = draw.Tint,
            });
        }

        _batch.End();
    }

    /// <summary>The position on the camera's pixel grid when pixel snapping is on; Z, the draw order, is kept.</summary>
    private Vector3 SnapKeepingDepth(Vector3 position)
    {
        var xy = _camera.Snap(new Vector2(position.X, position.Y));
        return new Vector3(xy.X, xy.Y, position.Z);
    }

    /// <summary>
    /// Where a layer is drawn once parallax is applied: its own position shifted by a fraction of the camera's. With parallax 0 the layer stays
    /// in the world; with 1 it is glued to the screen (the shift cancels the camera's movement exactly); in between it scrolls slower than the world.
    /// </summary>
    /// <param name="position">The layer entity's position (Z is draw depth and is kept).</param>
    /// <param name="camera">The camera's world position.</param>
    /// <param name="parallax">The layer's parallax factor per axis.</param>
    public static Vector3 ParallaxOrigin(Vector3 position, Vector2 camera, Vector2 parallax) =>
        new(position.X + camera.X * parallax.X, position.Y + camera.Y * parallax.Y, position.Z);
}
