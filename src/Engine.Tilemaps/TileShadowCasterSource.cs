using System.Numerics;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>
/// Turns the solid tiles of tile layers into walls that block light: for every solid tile, each side that touches a non-solid tile becomes a
/// <see cref="ShadowEdge"/> (counter-clockwise, so it faces out of the tile). Only layers whose <see cref="TileLayerRenderer.CastsShadows"/> is
/// set are read, and only the tiles near the light, so the cost follows the light's size and not the level's. Add it to
/// <see cref="LightingEffect.ShadowSources"/>.
/// </summary>
public sealed class TileShadowCasterSource : IShadowCasterSource
{
    private readonly World _world;
    private readonly AssetManager _assets;
    private readonly RenderClock? _clock;
    private readonly EntityAction<Transform, TileLayerRenderer> _collect;
    private List<ShadowEdge> _edges = null!;
    private Vector2 _min;
    private Vector2 _max;

    /// <summary>Creates the source.</summary>
    /// <param name="world">The world holding the tile layer entities.</param>
    /// <param name="assets">Resolves the layer and tile set handles.</param>
    /// <param name="clock">Interpolation factor, matching how the layers are drawn; null reads simulated transforms.</param>
    public TileShadowCasterSource(World world, AssetManager assets, RenderClock? clock = null)
    {
        _world = world;
        _assets = assets;
        _clock = clock;
        _collect = Collect; // Created once so the per-light query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void CollectEdges(Vector2 min, Vector2 max, List<ShadowEdge> edges)
    {
        _edges = edges;
        _min = min;
        _max = max;
        _world.Query(_collect);
    }

    /// <summary>Appends the walls of the solid cells in an inclusive cell range.</summary>
    /// <param name="layer">The tile layer.</param>
    /// <param name="tileSet">Decides which tiles are solid.</param>
    /// <param name="origin">World position of cell (0, 0)'s bottom-left corner.</param>
    /// <param name="fromX">First column.</param>
    /// <param name="fromY">First row.</param>
    /// <param name="toX">Last column.</param>
    /// <param name="toY">Last row.</param>
    /// <param name="edges">The list to append to.</param>
    public static void AddCellRange(TileLayer layer, TileSet tileSet, Vector2 origin, int fromX, int fromY, int toX, int toY, List<ShadowEdge> edges)
    {
        float size = layer.TileSize;
        for (int y = fromY; y <= toY; y++)
        {
            for (int x = fromX; x <= toX; x++)
            {
                if (!tileSet.IsSolid(layer.GetTile(x, y))) continue;

                var bottomLeft = origin + new Vector2(x * size, y * size);
                var bottomRight = bottomLeft + new Vector2(size, 0f);
                var topRight = bottomLeft + new Vector2(size, size);
                var topLeft = bottomLeft + new Vector2(0f, size);

                // A wall is only needed where light could actually reach the other side: next to an open cell.
                if (!tileSet.IsSolid(layer.GetTile(x, y - 1))) edges.Add(new ShadowEdge(bottomLeft, bottomRight));
                if (!tileSet.IsSolid(layer.GetTile(x + 1, y))) edges.Add(new ShadowEdge(bottomRight, topRight));
                if (!tileSet.IsSolid(layer.GetTile(x, y + 1))) edges.Add(new ShadowEdge(topRight, topLeft));
                if (!tileSet.IsSolid(layer.GetTile(x - 1, y))) edges.Add(new ShadowEdge(topLeft, bottomLeft));
            }
        }
    }

    private void Collect(Entity entity, ref Transform simulated, ref TileLayerRenderer renderer)
    {
        if (!renderer.CastsShadows || !renderer.Layer.IsValid || !renderer.TileSet.IsValid) return;

        var layer = _assets.Get(renderer.Layer);
        var tileSet = _assets.Get(renderer.TileSet);
        var position = TransformHierarchy.GetRenderTransform(_world, entity, simulated, _clock).Position;
        var origin = new Vector2(position.X, position.Y);

        // One cell of margin on every side, so the walls of cells just outside the area are included too.
        float size = layer.TileSize;
        int fromX = (int)MathF.Floor((_min.X - origin.X) / size) - 1;
        int fromY = (int)MathF.Floor((_min.Y - origin.Y) / size) - 1;
        int toX = (int)MathF.Floor((_max.X - origin.X) / size) + 1;
        int toY = (int)MathF.Floor((_max.Y - origin.Y) / size) + 1;
        AddCellRange(layer, tileSet, origin, fromX, fromY, toX, toY, _edges);
    }
}
