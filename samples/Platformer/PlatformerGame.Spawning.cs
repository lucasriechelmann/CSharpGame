using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Rendering;

namespace Platformer;

// Creating the level's entities: backdrop, tiles, hero, creatures.
public sealed partial class PlatformerGame
{
    private const float HeroSpriteSize = 40f;
    private const float CreatureSpriteSize = 32f;

    private void SpawnBackdrop()
    {
        var backdrop = World.CreateEntity();
        var center = LevelLayout.Origin + new Vector2(LevelLayout.Columns, LevelLayout.RowCount) * LevelLayout.TileSize / 2f;
        World.Add(backdrop, Transform.At(center.X, center.Y, -10));

        var sprite = Sprite.Create(_white, new Vector2(LevelLayout.Columns, LevelLayout.RowCount) * LevelLayout.TileSize);
        sprite.Color = new Color(0.13f, 0.15f, 0.22f);
        World.Add(backdrop, sprite);
    }

    /// <summary>Spawns one sprite per solid tile. (Phase 6 replaces this with chunked tilemap rendering.)</summary>
    private void SpawnTiles()
    {
        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                if (!_grid.IsSolid(x, y)) continue;

                var center = _grid.CellCenter(x, y);
                var tile = World.CreateEntity();
                World.Add(tile, Transform.At(center.X, center.Y, -5));

                var sprite = Sprite.Create(_white, new Vector2(_grid.TileSize));
                sprite.Color = (x + y) % 2 == 0 ? new Color(0.36f, 0.40f, 0.55f) : new Color(0.32f, 0.36f, 0.50f);
                World.Add(tile, sprite);
            }
        }
    }

    /// <summary>Spawns the hero just above the given tile column/row (row 0 is the bottom), so it drops onto the floor.</summary>
    private Entity SpawnHero(int column, int row)
    {
        // Collider is smaller than the sprite and nudged down so its bottom lines up with the creature's feet.
        var bottom = _grid.CellBounds(column, row).Min;
        var hero = World.CreateEntity();
        World.Add(hero, Transform.At(bottom.X + _grid.TileSize / 2f, bottom.Y + 22f, 10));
        World.Add(hero, CreatureSprite(cell: 0, size: HeroSpriteSize));
        World.Add(hero, PhysicsBody.Create());
        World.Add(hero, new BoxCollider { Size = new Vector2(24f, 30f), Offset = new Vector2(0f, -2.5f) });
        World.Add(hero, PlatformerController.Create(PlatformerTuning.Default));
        World.Add(hero, new PlatformerIntent());
        return hero;
    }

    private void SpawnInitialCreatures()
    {
        for (int i = 0; i < 12; i++)
        {
            float x = Range(_grid.Origin.X + 64f, _grid.Origin.X + _grid.Width * _grid.TileSize - 64f);
            float y = Range(0f, 200f);
            SpawnCreature(new Vector2(x, y));
        }
    }

    private void SpawnCreaturesAt(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
            SpawnCreature(position + new Vector2(Range(-20f, 20f), Range(-10f, 10f)));
    }

    /// <summary>Spawns a creature that falls, lands, and patrols. Spawning inside a solid tile lets it pass through that tile (bodies only collide with surfaces ahead of them).</summary>
    private void SpawnCreature(Vector2 position)
    {
        var creature = World.CreateEntity();
        World.Add(creature, Transform.At(position.X, position.Y, 5));
        World.Add(creature, CreatureSprite(cell: _random.Next(1, SheetColumns * SheetRows), size: CreatureSpriteSize));
        World.Add(creature, PhysicsBody.Create());
        World.Add(creature, new BoxCollider { Size = new Vector2(20f, 20f), Offset = new Vector2(0f, -4f) });
        World.Add(creature, new Patrol { Speed = Range(40f, 110f), Direction = _random.Next(2) == 0 ? -1f : 1f });
    }

    /// <summary>Builds a sprite showing one 16x16 cell of the creature sheet (cells run left to right, top to bottom).</summary>
    private Sprite CreatureSprite(int cell, float size)
    {
        int column = cell % SheetColumns;
        int row = cell / SheetColumns;

        var sprite = Sprite.Create(_sheet, new Vector2(size, size));
        sprite.Source = new Rect(column * CellSize, row * CellSize, CellSize, CellSize);
        return sprite;
    }

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
}
