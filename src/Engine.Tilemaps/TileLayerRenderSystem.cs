using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>
/// Draws every <see cref="TileLayerRenderer"/>, visiting only the chunks and tiles the camera can see. Tile layers are drawn
/// in their own pass: register one instance with <c>foreground: false</c> before the sprite system (background and ground)
/// and one with <c>foreground: true</c> after it (decoration in front of characters). Layers within a pass are ordered by Z.
/// </summary>
public sealed class TileLayerRenderSystem : ISystem
{
    /// <summary>
    /// Pixels trimmed from each side of a tile's atlas rectangle. With nearest filtering a sample can land exactly on the
    /// boundary and pick a neighbouring tile's pixels; this keeps every sample strictly inside the tile.
    /// </summary>
    private const float AtlasInsetPixels = 0.02f;

    private static readonly Comparison<TileLayerDraw> ByDepth = (a, b) => a.Origin.Z.CompareTo(b.Origin.Z);

    private readonly SpriteBatch _batch;
    private readonly Camera2D _camera;
    private readonly AssetManager _assets;
    private readonly bool _foreground;
    private readonly List<TileLayerDraw> _layers = new();
    private readonly EntityAction<Transform, TileLayerRenderer> _collect;

    /// <summary>Creates the system.</summary>
    /// <param name="batch">The batch tiles are drawn with.</param>
    /// <param name="camera">The camera that defines what is visible.</param>
    /// <param name="assets">Resolves layer, tile set and texture handles.</param>
    /// <param name="foreground">Which pass this instance draws: false for behind sprites, true for in front.</param>
    public TileLayerRenderSystem(SpriteBatch batch, Camera2D camera, AssetManager assets, bool foreground = false)
    {
        _batch = batch;
        _camera = camera;
        _assets = assets;
        _foreground = foreground;
        _collect = Collect; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _layers.Clear();
        world.Query(_collect);
        if (_layers.Count == 0) return;

        _layers.Sort(ByDepth);
        _camera.GetVisibleBounds(out var visibleMin, out var visibleMax);

        _batch.Begin(_camera.GetViewProjection());
        foreach (var layer in _layers)
            DrawLayer(layer, visibleMin, visibleMax);
        _batch.End();
    }

    private void Collect(Entity entity, ref Transform transform, ref TileLayerRenderer renderer)
    {
        if (renderer.Foreground != _foreground || !renderer.Layer.IsValid || !renderer.TileSet.IsValid) return;

        var tileSet = _assets.Get(renderer.TileSet);
        _layers.Add(new TileLayerDraw
        {
            Origin = transform.Position,
            Layer = _assets.Get(renderer.Layer),
            TileSet = tileSet,
            Texture = _assets.Get(tileSet.Texture),
            Tint = renderer.Tint,
        });
    }

    private void DrawLayer(in TileLayerDraw draw, Vector2 visibleMin, Vector2 visibleMax)
    {
        var layer = draw.Layer;
        float tileSize = layer.TileSize;

        // Visible cell range, in layer space.
        int minCellX = layer.CellIndex(visibleMin.X - draw.Origin.X);
        int maxCellX = layer.CellIndex(visibleMax.X - draw.Origin.X);
        int minCellY = layer.CellIndex(visibleMin.Y - draw.Origin.Y);
        int maxCellY = layer.CellIndex(visibleMax.Y - draw.Origin.Y);

        for (int chunkY = TileLayer.ChunkIndex(minCellY); chunkY <= TileLayer.ChunkIndex(maxCellY); chunkY++)
        {
            for (int chunkX = TileLayer.ChunkIndex(minCellX); chunkX <= TileLayer.ChunkIndex(maxCellX); chunkX++)
            {
                if (!layer.TryGetChunkTiles(chunkX, chunkY, out var tiles)) continue;

                int firstX = chunkX * TileLayer.ChunkSize;
                int firstY = chunkY * TileLayer.ChunkSize;
                for (int local = 0; local < tiles.Length; local++)
                {
                    int tileId = tiles[local];
                    if (tileId == 0) continue;

                    int cellX = firstX + local % TileLayer.ChunkSize;
                    int cellY = firstY + local / TileLayer.ChunkSize;
                    if (cellX < minCellX || cellX > maxCellX || cellY < minCellY || cellY > maxCellY) continue;

                    _batch.Draw(draw.Texture, new SpriteDraw
                    {
                        Position = new Vector3(
                            draw.Origin.X + (cellX + 0.5f) * tileSize,
                            draw.Origin.Y + (cellY + 0.5f) * tileSize,
                            draw.Origin.Z),
                        Size = new Vector2(tileSize, tileSize),
                        Pivot = new Vector2(0.5f, 0.5f),
                        UvRect = draw.TileSet.GetSourceRect(tileId).Inset(AtlasInsetPixels).ToUv(draw.Texture.Width, draw.Texture.Height),
                        Color = draw.Tint,
                    });
                }
            }
        }
    }
}
