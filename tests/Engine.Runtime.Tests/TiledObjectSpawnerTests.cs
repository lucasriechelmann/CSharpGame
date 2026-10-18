using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Runtime.Tiled;
using Engine.Tilemaps.Tiled;

namespace Engine.Runtime.Tests;

public class TiledObjectSpawnerTests
{
    private readonly World _world = new();
    private readonly List<string> _instantiated = new();

    private Entity Instantiate(string prefab)
    {
        _instantiated.Add(prefab);
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(new Vector3(0f, 0f, 8f))); // a prefab's own draw depth
        return e;
    }

    private static ImportedObject Obj(string cls, Vector2 position, Vector2 size = default, string name = "") =>
        new("things", name, cls, position, size, 0, size == Vector2.Zero, TiledProperties.Empty);

    private static ImportedMap Map(params ImportedObject[] objects) =>
        new([], [], objects, new Vector2(100f, 100f), 32f);

    private static readonly Dictionary<string, string> Prefabs = new() { ["Coin"] = "prefabs/coin.prefab.json" };

    [Fact]
    public void A_mapped_object_becomes_a_prefab_instance_at_its_centre_keeping_the_prefab_depth()
    {
        var map = Map(Obj("Coin", new Vector2(10f, 20f), new Vector2(32f, 32f)));

        var result = TiledObjectSpawner.Spawn(_world, Instantiate, map, Prefabs, Vector2.Zero);

        var spawned = Assert.Single(result.Spawned);
        Assert.Equal(["prefabs/coin.prefab.json"], _instantiated);
        Assert.Equal(new Vector3(26f, 36f, 8f), _world.Get<Transform>(spawned).Position);
    }

    [Fact]
    public void A_point_is_placed_exactly_and_the_offset_moves_everything()
    {
        var map = Map(Obj("Coin", new Vector2(5f, 6f)));

        var result = TiledObjectSpawner.Spawn(_world, Instantiate, map, Prefabs, new Vector2(100f, -50f));

        Assert.Equal(new Vector3(105f, -44f, 8f), _world.Get<Transform>(result.Spawned[0]).Position);
    }

    [Fact]
    public void Unknown_classes_are_handed_back_for_the_game_to_handle()
    {
        var start = Obj("PlayerStart", new Vector2(1f, 1f));
        var map = Map(Obj("Coin", Vector2.Zero), start, Obj("Coin", Vector2.One));

        var result = TiledObjectSpawner.Spawn(_world, Instantiate, map, Prefabs, Vector2.Zero);

        Assert.Equal(2, result.Spawned.Count);
        Assert.Equal([start], result.Unmapped);
    }

    [Fact]
    public void A_named_object_names_its_entity()
    {
        var map = Map(Obj("Coin", Vector2.Zero, name: "Bonus coin"));

        var result = TiledObjectSpawner.Spawn(_world, Instantiate, map, Prefabs, Vector2.Zero);

        Assert.Equal("Bonus coin", _world.Get<Name>(result.Spawned[0]).Value);
    }

    [Fact]
    public void An_unnamed_object_leaves_the_prefab_name_alone()
    {
        var map = Map(Obj("Coin", Vector2.Zero));

        var result = TiledObjectSpawner.Spawn(_world, Instantiate, map, Prefabs, Vector2.Zero);

        Assert.False(_world.Has<Name>(result.Spawned[0]));
    }

    [Fact]
    public void Missing_arguments_throw()
    {
        var map = Map();

        Assert.Throws<ArgumentNullException>(() => TiledObjectSpawner.Spawn(null!, Instantiate, map, Prefabs, Vector2.Zero));
        Assert.Throws<ArgumentNullException>(() => TiledObjectSpawner.Spawn(_world, null!, map, Prefabs, Vector2.Zero));
        Assert.Throws<ArgumentNullException>(() => TiledObjectSpawner.Spawn(_world, Instantiate, null!, Prefabs, Vector2.Zero));
        Assert.Throws<ArgumentNullException>(() => TiledObjectSpawner.Spawn(_world, Instantiate, map, null!, Vector2.Zero));
    }
}
