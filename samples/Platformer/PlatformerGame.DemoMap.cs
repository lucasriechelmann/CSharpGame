using System.Numerics;
using Engine.Core.Components;
using Engine.Runtime.Tiled;
using Engine.Tilemaps.Tiled;

namespace Platformer;

// A Tiled map (Assets/maps/demo.tmj) imported at run time: flipped tiles, an animated tile and a parallax layer, plus an object layer whose Gem
// points become prefab instances (TiledObjectSpawner). Spawned below the level.
public sealed partial class PlatformerGame
{
    /// <summary>Where the demo map's bottom-left corner lands in the world: well below the level, which spans roughly y = -300 and up.</summary>
    private static readonly Vector2 DemoMapOrigin = new(-300f, -1000f);

    /// <summary>Which prefab each Tiled object class stands for; classes not listed (the player start) are left for the game.</summary>
    private static readonly Dictionary<string, string> DemoMapPrefabs = new() { ["Gem"] = "prefabs/gem.prefab.json" };

    private bool _demoMapLoaded;
    private bool _viewingDemoMap;

    /// <summary>Imports the demo map the first time, then flips the camera between the map and the hero.</summary>
    private void ToggleDemoMap()
    {
        if (!_demoMapLoaded)
        {
            var options = new TiledImportOptions { WorldUnitsPerPixel = 2f }; // 16 px tiles become 32 world units, like the level's
            var map = TiledConverter.Convert(TiledReader.ReadFile(Path.Combine(Launch.AssetRoot, "maps", "demo.tmj")), options);
            foreach (var entity in TiledSpawner.Spawn(World, Assets, map, "maps/demo", baseDepth: 12f))
            {
                ref var transform = ref World.Get<Transform>(entity);
                transform.Position += new Vector3(DemoMapOrigin.X, DemoMapOrigin.Y, 0f);
            }

            var objects = TiledObjectSpawner.Spawn(World, Scenes.Instantiate, map, DemoMapPrefabs, DemoMapOrigin);
            _demoMapLoaded = true;
            Log.Info("Game", $"Imported maps/demo.tmj: {map.Layers.Count} layers, {map.TileSets.Count} tile set(s), {objects.Spawned.Count} object(s) from prefabs, {objects.Unmapped.Count} left to the game");
        }

        _viewingDemoMap = !_viewingDemoMap;
        if (_viewingDemoMap) _camera.Position = DemoMapOrigin + new Vector2(224f, 96f); // the middle of the 14x6 tile map
    }
}
