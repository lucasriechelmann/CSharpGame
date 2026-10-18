using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Rendering;

namespace Engine.Tilemaps.Tiled;

/// <summary>
/// Puts an <see cref="ImportedMap"/> into a running game: loads each tile set's texture, registers the tile sets and tile layers as assets under
/// a prefix, and creates one entity per visible layer with a <c>Name</c>, a <c>Transform</c> and a <see cref="TileLayerRenderer"/> (parallax,
/// foreground flag and opacity included). Objects are not spawned: what they mean is up to the game, so they stay in the imported map.
/// </summary>
public static class TiledSpawner
{
    /// <summary>Creates the layer entities of a map.</summary>
    /// <param name="world">The world to add the entities to.</param>
    /// <param name="assets">Loads the textures and registers the tile sets and layers.</param>
    /// <param name="map">The converted map.</param>
    /// <param name="assetPrefix">Prefix for the asset names, such as <c>maps/demo</c>: the layers become <c>maps/demo/layers/ground</c> and so on. Use a different prefix for every map spawned.</param>
    /// <param name="baseDepth">The Z of the bottom layer (a larger Z draws in front).</param>
    /// <param name="depthStep">How much further forward each following layer is.</param>
    /// <param name="includeHidden">True also spawns layers that are hidden in Tiled.</param>
    /// <returns>The layer entities, bottom first.</returns>
    /// <exception cref="InvalidOperationException">A tile set or layer name is already registered under that prefix.</exception>
    public static IReadOnlyList<Entity> Spawn(World world, AssetManager assets, ImportedMap map, string assetPrefix, float baseDepth = 0f, float depthStep = 0.01f, bool includeHidden = false)
    {
        var tileSetHandles = new Dictionary<string, AssetHandle<TileSet>>();
        foreach (var imported in map.TileSets)
        {
            var texture = assets.Load<Texture2D>(imported.TextureName);
            var atlas = assets.Get(texture);
            var tileSet = imported.Data.CreateTileSet(texture, atlas.Width, atlas.Height);
            tileSetHandles[imported.Name] = assets.Add($"{assetPrefix}/tilesets/{imported.Name}", tileSet);
        }

        var entities = new List<Entity>();
        foreach (var layer in map.Layers)
        {
            if (!layer.Visible && !includeHidden) continue;

            var handle = assets.Add($"{assetPrefix}/layers/{layer.Name}", layer.Layer);
            var renderer = TileLayerRenderer.Create(handle, tileSetHandles[layer.TileSetName], layer.Foreground);
            renderer.Parallax = layer.Parallax;
            renderer.Tint = new Color(1f, 1f, 1f, layer.Opacity);

            var entity = world.CreateEntity();
            world.Add(entity, Name.Of(layer.Name));
            world.Add(entity, Transform.At(new Vector3(layer.Position, baseDepth + layer.Order * depthStep)));
            world.Add(entity, renderer);
            entities.Add(entity);
        }

        return entities;
    }
}
