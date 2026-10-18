using System.Text.Json.Nodes;
using Engine.Core.Components;
using Engine.Scenes.Tests.Fixtures;

namespace Engine.Scenes.Tests;

/// <summary>Saving keeps prefab instances as a prefab name plus the differences, instead of expanding every component.</summary>
public sealed class PrefabSaveTests : IDisposable
{
    private const string CreaturePrefab = """
        { "components": {
            "Name": { "value": "Creature" },
            "Transform": { "position": { "x": 0, "y": 0, "z": 5 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "scale": { "x": 2, "y": 2, "z": 1 } },
            "Health": { "value": 10 }
        } }
        """;

    private const string Scene = """
        { "version": 1, "entities": [
            { "prefab": "prefabs/creature.prefab.json", "components": { "Transform": { "position": { "x": 100, "y": 50 } } } }
        ] }
        """;

    private readonly SceneTestBed _bed = new();

    public PrefabSaveTests() => _bed.Write("prefabs/creature.prefab.json", CreaturePrefab);

    public void Dispose() => _bed.Dispose();

    private JsonObject SavedEntry(int index = 0) => (JsonObject)((JsonArray)JsonNode.Parse(_bed.Scenes.Save())!["entities"]!)[index]!;

    [Fact]
    public void An_entity_loaded_from_a_prefab_remembers_it()
    {
        var entity = Assert.Single(_bed.Scenes.Load(Scene));

        Assert.Equal("prefabs/creature.prefab.json", _bed.World.Get<PrefabLink>(entity).Prefab);
    }

    [Fact]
    public void An_instantiated_prefab_remembers_it_too()
    {
        var entity = _bed.Scenes.Instantiate("prefabs/creature.prefab.json");

        Assert.Equal("prefabs/creature.prefab.json", _bed.World.Get<PrefabLink>(entity).Prefab);
    }

    [Fact]
    public void An_entity_not_made_from_a_prefab_has_no_link()
    {
        var entity = Assert.Single(_bed.Scenes.Load("""{ "version": 1, "entities": [ { "components": { "Health": { "value": 1 } } } ] }"""));

        Assert.False(_bed.World.Has<PrefabLink>(entity));
    }

    [Fact]
    public void Saving_writes_the_prefab_name_and_only_the_overridden_fields()
    {
        _bed.Scenes.Load(Scene);

        var entry = SavedEntry();

        Assert.Equal("prefabs/creature.prefab.json", (string)entry["prefab"]!);
        var components = (JsonObject)entry["components"]!;
        Assert.Equal(new[] { "Transform" }, components.Select(c => c.Key).ToArray());
        var position = (JsonObject)((JsonObject)components["Transform"]!)["position"]!;
        Assert.Equal(new[] { "x", "y" }, position.Select(p => p.Key).ToArray()); // z is the prefab's, so it is not repeated
        Assert.Equal(100f, (float)position["x"]!);
        Assert.False(((JsonObject)components["Transform"]!).ContainsKey("scale"));
    }

    [Fact]
    public void An_unchanged_instance_is_saved_as_just_the_prefab_name()
    {
        _bed.Scenes.Instantiate("prefabs/creature.prefab.json");

        var entry = SavedEntry();

        Assert.Equal("prefabs/creature.prefab.json", (string)entry["prefab"]!);
        Assert.False(entry.ContainsKey("components"));
    }

    [Fact]
    public void An_edit_made_after_loading_is_saved_as_an_override()
    {
        var entity = _bed.Scenes.Instantiate("prefabs/creature.prefab.json");
        _bed.World.Get<Health>(entity).Value = 99;

        var components = (JsonObject)SavedEntry()["components"]!;

        Assert.Equal(99, (int)((JsonObject)components["Health"]!)["value"]!);
        Assert.Single(components);
    }

    [Fact]
    public void A_removed_component_is_saved_as_null()
    {
        var entity = _bed.Scenes.Instantiate("prefabs/creature.prefab.json");
        _bed.World.Remove<Health>(entity);

        var components = (JsonObject)SavedEntry()["components"]!;

        Assert.True(components.ContainsKey("Health"));
        Assert.Null(components["Health"]);
    }

    [Fact]
    public void A_component_added_to_an_instance_is_saved_in_full()
    {
        var entity = _bed.Scenes.Instantiate("prefabs/creature.prefab.json");
        _bed.World.Add(entity, new Fixtures.Stance { Mode = StanceMode.Crouching });

        var components = (JsonObject)SavedEntry()["components"]!;

        Assert.Equal("Crouching", (string)((JsonObject)components["Stance"]!)["mode"]!);
    }

    [Fact]
    public void An_entity_without_a_link_is_still_saved_in_full()
    {
        var entity = _bed.World.CreateEntity();
        _bed.World.Add(entity, new Health { Value = 4 });

        var entry = SavedEntry();

        Assert.False(entry.ContainsKey("prefab"));
        Assert.Equal(4, (int)((JsonObject)((JsonObject)entry["components"]!)["Health"]!)["value"]!);
    }

    [Fact]
    public void The_link_itself_is_never_written_as_a_component()
    {
        _bed.Scenes.Load(Scene);

        Assert.DoesNotContain("PrefabLink", _bed.Scenes.Save());
    }

    [Fact]
    public void Saving_and_loading_keeps_the_instance_and_its_values()
    {
        _bed.Scenes.Load(Scene);
        string saved = _bed.Scenes.Save();
        _bed.World.Clear();

        var entity = Assert.Single(_bed.Scenes.Load(saved));

        Assert.Equal(100f, _bed.World.Get<Transform>(entity).Position.X);
        Assert.Equal(5f, _bed.World.Get<Transform>(entity).Position.Z);
        Assert.Equal(10, _bed.World.Get<Health>(entity).Value);
        Assert.Equal("prefabs/creature.prefab.json", _bed.World.Get<PrefabLink>(entity).Prefab);
    }

    [Fact]
    public void A_saved_instance_follows_a_later_change_to_its_prefab()
    {
        _bed.Scenes.Load(Scene);
        string saved = _bed.Scenes.Save();

        // A fresh session (its own asset cache) where the prefab's health was edited after the scene was saved:
        // the instance did not override it, so it follows the prefab.
        using var later = new SceneTestBed();
        later.Write("prefabs/creature.prefab.json", CreaturePrefab.Replace("\"value\": 10", "\"value\": 25"));
        var entity = Assert.Single(later.Scenes.Load(saved));

        Assert.Equal(25, later.World.Get<Health>(entity).Value);
        Assert.Equal(100f, later.World.Get<Transform>(entity).Position.X); // the override still applies
    }

    [Fact]
    public void Saving_twice_gives_the_same_text()
    {
        _bed.Scenes.Load(Scene);

        Assert.Equal(_bed.Scenes.Save(), _bed.Scenes.Save());
    }

    [Fact]
    public void Several_instances_of_one_prefab_are_each_diffed_against_it()
    {
        var first = _bed.Scenes.Instantiate("prefabs/creature.prefab.json");
        _bed.Scenes.Instantiate("prefabs/creature.prefab.json");
        _bed.World.Get<Health>(first).Value = 3;

        Assert.Equal(3, (int)((JsonObject)((JsonObject)SavedEntry(0)["components"]!)["Health"]!)["value"]!);
        Assert.False(SavedEntry(1).ContainsKey("components"));
    }
}
