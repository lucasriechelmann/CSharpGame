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

    private void SpawnCreaturesAt(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var spot = position + new Vector2(Range(-20f, 20f), Range(-10f, 10f));
            if (IsFree(spot)) SpawnCreature(spot);
        }

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
    }

    /// <summary>Replaces the whole world with the quick-save, then re-finds the hero and snaps the camera to it.</summary>
    private void QuickLoad()
    {
        if (!File.Exists(QuickSavePath))
        {
            ShowStatus("No quick-save yet (press F5)");
            return;
        }

        World.Clear();
        Scenes.LoadFile(QuickSavePath);
        _hero = FindHero();
        SnapCameraToHero();
        ShowStatus($"Loaded {World.EntityCount} entities");
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
