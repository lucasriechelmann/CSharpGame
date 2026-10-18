using System.Numerics;
using Engine.Animation;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Platformer;

// Scene data: loading the level, finding the entities the game needs, spawning from prefabs, quick save and load.
public sealed partial class PlatformerGame
{
    private const string LevelScene = "scenes/level.scene.json";
    private const string CreaturePrefab = "prefabs/creature.prefab.json";
    private const string GroundLayer = "levels/ground.layer.json";
    private const string HeroAnimation = "anim/hero.anim.json";
    private const int CreatureTypeCount = 8;
    private const float CreatureHalfExtent = 12f;

    // Written next to the executable, outside the asset folder, because it is player data rather than content.
    private static readonly string QuickSavePath = Path.Combine(AppContext.BaseDirectory, "saves", "quicksave.scene.json");

    private AssetHandle<AnimationStateMachine>[] _creatureMachines = [];
    private string _status = "";
    private double _statusSecondsLeft;

    /// <summary>The hero's simulated position in the world (not the interpolated draw position).</summary>
    private Vector2 HeroPosition()
    {
        var position = World.Get<Transform>(_hero).Position;
        return new Vector2(position.X, position.Y);
    }

    /// <summary>The hero is the one entity that has a <see cref="PlatformerIntent"/>.</summary>
    private Entity FindHero()
    {
        Entity hero = Entity.Null;
        World.Query<PlatformerIntent>((Entity entity, ref PlatformerIntent _) => hero = entity);
        return hero.IsNull ? throw new InvalidOperationException("The scene has no hero (an entity with a PlatformerIntent).") : hero;
    }

    /// <summary>
    /// Builds the physics grid from the ground tile layer. The layer entity's position says where the layer sits in the world,
    /// so level placement lives in the scene file, not in code.
    /// </summary>
    private TileGrid BuildCollisionGrid()
    {
        var groundHandle = Assets.Load<TileLayer>(GroundLayer);
        var origin = Vector2.Zero;
        var tileSetHandle = AssetHandle<TileSet>.Invalid;
        World.Query<Transform, TileLayerRenderer>((Entity _, ref Transform transform, ref TileLayerRenderer renderer) =>
        {
            if (renderer.Layer != groundHandle) return;
            origin = new Vector2(transform.Position.X, transform.Position.Y);
            tileSetHandle = renderer.TileSet;
        });

        if (!tileSetHandle.IsValid) throw new InvalidOperationException($"The scene has no tile layer entity for '{GroundLayer}'.");
        return TileGridBuilder.FromLayer(Assets.Get(groundHandle), Assets.Get(tileSetHandle), origin);
    }

    /// <summary>Rebuilds collision from the ground layer as it is now and hands it to the physics system.</summary>
    private void RebuildCollisionGrid()
    {
        _grid = BuildCollisionGrid();
        _physics.Grid = _grid;
    }

    private void SpawnCreaturesAt(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var spot = position + new Vector2(Range(-20f, 20f), Range(-10f, 10f));
            if (IsFree(spot)) SpawnCreature(spot);
        }

        Log.Debug("Game", $"Spawn request near ({position.X:F0}, {position.Y:F0}), up to {count} creatures");

        // One blip per click, not per creature, with a random pitch so repeated clicks do not sound identical.
        Audio.Play(_spawnSound, volume: 0.5f, pitch: Range(0.9f, 1.2f));
    }

    /// <summary>Instantiates the creature prefab, then varies this instance: position, creature type, walk phase, speed and direction.</summary>
    private void SpawnCreature(Vector2 position)
    {
        var creature = Scenes.Instantiate(CreaturePrefab);

        World.Get<Transform>(creature).Position = new Vector3(position.X, position.Y, 5f);

        ref var animator = ref World.Get<SpriteAnimator>(creature);
        animator.Machine = _creatureMachines[_random.Next(CreatureTypeCount)];
        animator.StateTime = Range(0f, 1f); // desynchronise the walk cycles

        ref var patrol = ref World.Get<Patrol>(creature);
        patrol.Speed = Range(40f, 110f);
        patrol.Direction = _random.Next(2) == 0 ? -1f : 1f;
    }

    /// <summary>
    /// True when a creature-sized box around <paramref name="position"/> touches no solid tile. Bodies only collide with
    /// surfaces ahead of them, so one spawned inside a tile would fall straight through it.
    /// </summary>
    private bool IsFree(Vector2 position)
    {
        foreach (float dx in (ReadOnlySpan<float>)[-CreatureHalfExtent, CreatureHalfExtent])
            foreach (float dy in (ReadOnlySpan<float>)[-CreatureHalfExtent, CreatureHalfExtent])
                if (_grid.IsSolid(_grid.CellX(position.X + dx), _grid.CellY(position.Y + dy)))
                    return false;

        return true;
    }

    private void QuickSave()
    {
        Scenes.SaveToFile(QuickSavePath);
        ShowStatus($"Saved {World.EntityCount} entities");
        Log.Info("Game", $"Quick-saved {World.EntityCount} entities to {QuickSavePath}");
    }

    /// <summary>Replaces the whole world with the quick-save, then re-finds the hero and snaps the camera to it.</summary>
    private void QuickLoad()
    {
        if (!File.Exists(QuickSavePath))
        {
            ShowStatus("No quick-save yet (press F5)");
            Log.Warning("Game", "Quick-load ignored: no quick-save file yet");
            return;
        }

        World.Clear();
        Scenes.LoadFile(QuickSavePath);
        AfterWorldReplaced();
        ShowStatus($"Loaded {World.EntityCount} entities");
        Log.Info("Game", $"Quick-loaded {World.EntityCount} entities");
    }

    /// <summary>
    /// Re-establishes what the game caches from the world after it was replaced by a saved scene (quick-load, or the editor's
    /// Reload): the hero, the camera, where the hero respawns, and how many coins are left.
    /// </summary>
    private void AfterWorldReplaced()
    {
        _hero = FindHero();
        SnapCameraToHero();
        _session.SetStart(HeroPosition()); // the loaded spot is where the hero comes back after a hit
        _session.SyncCoinsRemaining(CountCoins()); // the saved world holds only the coins not yet collected
        SetDoorOpen(_session.GoalUnlocked);
    }

    /// <inheritdoc />
    protected override Camera2D? SceneCamera => _camera;

    /// <inheritdoc />
    protected override string? ScenePath => EditableLevelPath;

    /// <inheritdoc />
    protected override void SceneReloaded() => AfterWorldReplaced();

    /// <summary>
    /// The level file the editor saves to: the copy in the project's <c>Assets</c> folder when the game was started from a build
    /// inside the project tree (so a save is a source change you can commit; the next build copies it to the output folder),
    /// otherwise the copy next to the executable.
    /// </summary>
    private static string EditableLevelPath => EditableAsset(LevelScene);

    /// <inheritdoc />
    protected override string EditableAssetPath(string assetName) => EditableAsset(assetName);

    /// <summary>The project's copy of an asset when the game runs from a build inside the project tree, otherwise the copy next to the executable.</summary>
    private static string EditableAsset(string assetName)
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Platformer.csproj")))
                return Path.GetFullPath(Path.Combine(folder.FullName, "Assets", assetName));
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Assets", assetName));
    }

    private void ShowStatus(string message)
    {
        _status = message;
        _statusSecondsLeft = 3.0;
    }

    private void UpdateStatusMessage(double dt)
    {
        if (_statusSecondsLeft > 0) _statusSecondsLeft -= dt;
    }

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
}
