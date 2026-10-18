using System.Numerics;
using Engine.Animation;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Rendering;
using Engine.Tilemaps;

namespace Platformer;

// Creating the level's entities: tile layers, hero, creatures.
public sealed partial class PlatformerGame
{
    private const float HeroSpriteSize = 40f;
    private const float CreatureSpriteSize = 32f;
    private const float CreatureHalfExtent = 12f;

    /// <summary>
    /// Registers the three tile layers as assets, spawns one entity per layer, and builds the physics grid from the ground layer.
    /// Layer depth: background -8, ground -5, foreground +20 (in front of characters at 5 and 10).
    /// </summary>
    private void SpawnTileLayers()
    {
        var ground = LevelLayout.CreateGround();
        var tileSet = LevelLayout.CreateTileSet(Assets, _tilesTexture);
        var tileSetHandle = Assets.Add("tileset/world", tileSet);

        SpawnLayer(Assets.Add("layer/background", LevelLayout.CreateBackground()), tileSetHandle, z: -8f, foreground: false);
        SpawnLayer(Assets.Add("layer/ground", ground), tileSetHandle, z: -5f, foreground: false);
        SpawnLayer(Assets.Add("layer/foreground", LevelLayout.CreateForeground()), tileSetHandle, z: 20f, foreground: true);

        _grid = TileGridBuilder.FromLayer(ground, tileSet, LevelLayout.Origin);
    }

    private void SpawnLayer(AssetHandle<TileLayer> layer, AssetHandle<TileSet> tileSet, float z, bool foreground)
    {
        var entity = World.CreateEntity();
        World.Add(entity, Transform.At(LevelLayout.Origin.X, LevelLayout.Origin.Y, z));
        World.Add(entity, TileLayerRenderer.Create(layer, tileSet, foreground));
    }

    /// <summary>Spawns the hero just above the given tile column/row (row 0 is the bottom), so it drops onto the floor.</summary>
    private Entity SpawnHero(int column, int row)
    {
        // The collider is smaller than the sprite and nudged down so its bottom lines up with the character's feet.
        var bottom = _grid.CellBounds(column, row).Min;
        var hero = World.CreateEntity();
        World.Add(hero, Transform.At(bottom.X + _grid.TileSize / 2f, bottom.Y + 22f, 10));
        World.Add(hero, Sprite.Create(_heroSheet, new Vector2(HeroSpriteSize, HeroSpriteSize)));
        World.Add(hero, SpriteAnimator.Create(Assets, _heroMachine));
        World.Add(hero, PhysicsBody.Create());
        World.Add(hero, new BoxCollider { Size = new Vector2(24f, 30f), Offset = new Vector2(0f, -2.5f) });
        World.Add(hero, PlatformerController.Create(PlatformerTuning.Default));
        World.Add(hero, new PlatformerIntent());
        return hero;
    }

    private void SpawnInitialCreatures()
    {
        for (int spawned = 0, attempts = 0; spawned < 12 && attempts < 200; attempts++)
        {
            var position = new Vector2(
                Range(_grid.Origin.X + 64f, _grid.Origin.X + _grid.Width * _grid.TileSize - 64f),
                Range(_grid.Origin.Y + 64f, _grid.Origin.Y + _grid.Height * _grid.TileSize - 64f));
            if (!IsFree(position)) continue;

            SpawnCreature(position);
            spawned++;
        }
    }

    private void SpawnCreaturesAt(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var spot = position + new Vector2(Range(-20f, 20f), Range(-10f, 10f));
            if (IsFree(spot)) SpawnCreature(spot);
        }
    }

    /// <summary>Spawns a creature that falls, lands and patrols, with a random type, speed, direction and animation phase.</summary>
    private void SpawnCreature(Vector2 position)
    {
        int type = _random.Next(CreatureAnimations.TypeCount);
        var animator = SpriteAnimator.Create(Assets, _creatureMachines[type]);
        animator.StateTime = Range(0f, 1f); // desynchronise the walk cycles

        var creature = World.CreateEntity();
        World.Add(creature, Transform.At(position.X, position.Y, 5));
        World.Add(creature, Sprite.Create(_creatureSheet, new Vector2(CreatureSpriteSize, CreatureSpriteSize)));
        World.Add(creature, animator);
        World.Add(creature, PhysicsBody.Create());
        World.Add(creature, new BoxCollider { Size = new Vector2(20f, 20f), Offset = new Vector2(0f, -4f) });
        World.Add(creature, new Patrol { Speed = Range(40f, 110f), Direction = _random.Next(2) == 0 ? -1f : 1f });
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

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
}
