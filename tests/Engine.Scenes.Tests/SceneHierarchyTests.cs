using System.Numerics;
using System.Text.Json.Nodes;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Scenes.Tests.Fixtures;

namespace Engine.Scenes.Tests;

public sealed class SceneHierarchyTests : IDisposable
{
    private readonly SceneTestBed _bed = new();

    public void Dispose() => _bed.Dispose();

    private Entity Make(string name, Vector3 position)
    {
        var entity = _bed.World.CreateEntity();
        _bed.World.Add(entity, Name.Of(name));
        _bed.World.Add(entity, Transform.At(position));
        return entity;
    }

    [Fact]
    public void A_child_is_saved_with_the_index_of_its_parent()
    {
        var parent = Make("parent", new Vector3(1, 0, 0));
        var child = Make("child", new Vector3(2, 0, 0));
        TransformHierarchy.SetParent(_bed.World, child, parent, keepWorld: false);

        var entities = JsonNode.Parse(_bed.Scenes.Save())!["entities"]!.AsArray();

        Assert.Null(entities[0]!["parent"]);
        Assert.Equal(0, entities[1]!["parent"]!.GetValue<int>());
    }

    [Fact]
    public void Parent_links_survive_a_save_and_load()
    {
        var parent = Make("parent", new Vector3(10, 0, 0));
        var child = Make("child", new Vector3(2, 0, 0));
        var grandchild = Make("grandchild", new Vector3(0, 1, 0));
        TransformHierarchy.SetParent(_bed.World, child, parent, keepWorld: false);
        TransformHierarchy.SetParent(_bed.World, grandchild, child, keepWorld: false);
        string json = _bed.Scenes.Save();

        using var other = new SceneTestBed();
        var loaded = other.Scenes.Load(json);

        Assert.Equal(loaded[0], TransformHierarchy.GetParent(other.World, loaded[1]));
        Assert.Equal(loaded[1], TransformHierarchy.GetParent(other.World, loaded[2]));
        Assert.Equal(new Vector3(12, 1, 0), TransformHierarchy.GetWorldTransform(other.World, loaded[2]).Position);
    }

    [Fact]
    public void A_child_listed_before_its_parent_still_links_up()
    {
        const string json = """
            { "version": 1, "entities": [
                { "parent": 1, "components": { "Transform": { "position": { "x": 1, "y": 0, "z": 0 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "scale": { "x": 1, "y": 1, "z": 1 } } } },
                { "components": { "Transform": { "position": { "x": 5, "y": 0, "z": 0 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "scale": { "x": 1, "y": 1, "z": 1 } } } }
            ] }
            """;

        var loaded = _bed.Scenes.Load(json);

        Assert.Equal(loaded[1], TransformHierarchy.GetParent(_bed.World, loaded[0]));
    }

    [Fact]
    public void Parent_is_not_written_as_a_component()
    {
        var parent = Make("parent", Vector3.Zero);
        var child = Make("child", Vector3.Zero);
        TransformHierarchy.SetParent(_bed.World, child, parent, keepWorld: false);

        var components = JsonNode.Parse(_bed.Scenes.Save())!["entities"]![1]!["components"]!.AsObject();

        Assert.False(components.ContainsKey("Parent"));
    }

    [Theory]
    [InlineData("""{ "version": 1, "entities": [ { "parent": 3 } ] }""")]
    [InlineData("""{ "version": 1, "entities": [ { "parent": -2 } ] }""")]
    [InlineData("""{ "version": 1, "entities": [ { "parent": 0 } ] }""")]
    [InlineData("""{ "version": 1, "entities": [ { "parent": 1 }, { "parent": 0 } ] }""")]
    public void Bad_parent_indices_fail_without_creating_entities(string json)
    {
        Assert.Throws<InvalidOperationException>(() => _bed.Scenes.Load(json));

        Assert.Equal(0, _bed.World.EntityCount);
    }
}
