using System.Text.Json.Nodes;
using Engine.Core.Components;
using Engine.Scenes.Tests.Fixtures;

namespace Engine.Scenes.Tests;

public sealed class PrefabTests : IDisposable
{
    private const string CreaturePrefab = """
        { "components": {
            "Name": { "value": "Creature" },
            "Transform": { "position": { "x": 0, "y": 0, "z": 5 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "scale": { "x": 2, "y": 2, "z": 1 } },
            "Health": { "value": 10 }
        } }
        """;

    private readonly SceneTestBed _bed = new();

    public PrefabTests()
    {
        _bed.Write("prefabs/creature.prefab.json", CreaturePrefab);
    }

    public void Dispose() => _bed.Dispose();

    [Fact]
    public void Instantiate_creates_an_entity_with_the_prefabs_components()
    {
        var e = _bed.Scenes.Instantiate("prefabs/creature.prefab.json");

        Assert.Equal("Creature", _bed.World.Get<Name>(e).Value);
        Assert.Equal(10, _bed.World.Get<Health>(e).Value);
        Assert.Equal(5f, _bed.World.Get<Transform>(e).Position.Z);
    }

    [Fact]
    public void A_scene_entry_can_override_part_of_a_component_and_keep_the_rest()
    {
        string scene = """
            { "version": 1, "entities": [
                { "prefab": "prefabs/creature.prefab.json",
                  "components": { "Transform": { "position": { "x": 100, "y": 50 } } } }
            ] }
            """;

        var e = Assert.Single(_bed.Scenes.Load(scene));

        var t = _bed.World.Get<Transform>(e);
        Assert.Equal(100f, t.Position.X);
        Assert.Equal(50f, t.Position.Y);
        Assert.Equal(5f, t.Position.Z);          // from the prefab
        Assert.Equal(2f, t.Scale.X);             // from the prefab
        Assert.Equal(1f, t.Rotation.W);          // from the prefab: a valid rotation, not default(Quaternion)
        Assert.Equal(10, _bed.World.Get<Health>(e).Value);
    }

    [Fact]
    public void Null_in_an_override_removes_a_component_from_the_instance()
    {
        string scene = """
            { "version": 1, "entities": [
                { "prefab": "prefabs/creature.prefab.json", "components": { "Health": null } }
            ] }
            """;

        var e = Assert.Single(_bed.Scenes.Load(scene));

        Assert.False(_bed.World.Has<Health>(e));
        Assert.True(_bed.World.Has<Name>(e));
    }

    [Fact]
    public void An_override_can_add_a_component_the_prefab_lacks()
    {
        string scene = """
            { "version": 1, "entities": [
                { "prefab": "prefabs/creature.prefab.json", "components": { "Stance": { "mode": "Crouching" } } }
            ] }
            """;

        var e = Assert.Single(_bed.Scenes.Load(scene));

        Assert.Equal(StanceMode.Crouching, _bed.World.Get<Stance>(e).Mode);
    }

    [Fact]
    public void Overrides_for_one_instance_do_not_leak_into_the_next()
    {
        var first = _bed.Scenes.Instantiate(new JsonObject
        {
            ["prefab"] = "prefabs/creature.prefab.json",
            ["components"] = new JsonObject { ["Health"] = new JsonObject { ["value"] = 999 } },
        });
        var second = _bed.Scenes.Instantiate("prefabs/creature.prefab.json");

        Assert.Equal(999, _bed.World.Get<Health>(first).Value);
        Assert.Equal(10, _bed.World.Get<Health>(second).Value); // the cached prefab was not modified
    }

    [Fact]
    public void A_prefab_can_be_based_on_another_prefab()
    {
        _bed.Write("prefabs/boss.prefab.json", """
            { "prefab": "prefabs/creature.prefab.json",
              "components": { "Name": { "value": "Boss" }, "Health": { "value": 500 } } }
            """);

        var e = _bed.Scenes.Instantiate("prefabs/boss.prefab.json");

        Assert.Equal("Boss", _bed.World.Get<Name>(e).Value);
        Assert.Equal(500, _bed.World.Get<Health>(e).Value);
        Assert.Equal(2f, _bed.World.Get<Transform>(e).Scale.X); // inherited from the grandparent
    }

    [Fact]
    public void A_prefab_cycle_is_detected()
    {
        _bed.Write("prefabs/a.prefab.json", """{ "prefab": "prefabs/b.prefab.json" }""");
        _bed.Write("prefabs/b.prefab.json", """{ "prefab": "prefabs/a.prefab.json" }""");

        Assert.Throws<InvalidOperationException>(() => _bed.Scenes.Instantiate("prefabs/a.prefab.json"));
        Assert.Equal(0, _bed.World.EntityCount);
    }

    [Fact]
    public void A_missing_prefab_throws_FileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => _bed.Scenes.Instantiate("prefabs/ghost.prefab.json"));
    }

    [Fact]
    public void An_invalid_prefab_file_is_reported()
    {
        _bed.Write("prefabs/broken.prefab.json", "{ not json");
        _bed.Write("prefabs/array.prefab.json", "[]");

        Assert.Throws<InvalidOperationException>(() => _bed.Scenes.Instantiate("prefabs/broken.prefab.json"));
        Assert.Throws<InvalidOperationException>(() => _bed.Scenes.Instantiate("prefabs/array.prefab.json"));
    }

    [Fact]
    public void A_serialized_entity_is_a_valid_prefab()
    {
        var original = _bed.World.CreateEntity();
        _bed.World.Add(original, Name.Of("Authored"));
        _bed.World.Add(original, new Health { Value = 77 });
        _bed.Write("prefabs/authored.prefab.json", _bed.Scenes.SerializeEntity(original).ToJsonString());

        var copy = _bed.Scenes.Instantiate("prefabs/authored.prefab.json");

        Assert.Equal("Authored", _bed.World.Get<Name>(copy).Value);
        Assert.Equal(77, _bed.World.Get<Health>(copy).Value);
        Assert.NotEqual(original, copy);
    }

    [Fact]
    public void Serializing_a_dead_entity_throws()
    {
        var e = _bed.World.CreateEntity();
        _bed.World.DestroyEntity(e);

        Assert.Throws<InvalidOperationException>(() => _bed.Scenes.SerializeEntity(e));
    }
}
