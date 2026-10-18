using System.Text.Json.Nodes;
using Engine.Scenes;

namespace Engine.Scenes.Tests;

public class JsonMergeTests
{
    private static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;

    [Fact]
    public void Scalars_are_replaced_and_new_keys_are_added()
    {
        var target = Obj("""{ "a": 1, "b": 2 }""");

        JsonMerge.Apply(target, Obj("""{ "b": 20, "c": 30 }"""));

        Assert.Equal(Obj("""{ "a": 1, "b": 20, "c": 30 }""").ToJsonString(), target.ToJsonString());
    }

    [Fact]
    public void Nested_objects_merge_instead_of_being_replaced()
    {
        var target = Obj("""{ "position": { "x": 1, "y": 2 }, "scale": { "x": 1 } }""");

        JsonMerge.Apply(target, Obj("""{ "position": { "x": 10 } }"""));

        Assert.Equal(10, target["position"]!["x"]!.GetValue<int>());
        Assert.Equal(2, target["position"]!["y"]!.GetValue<int>()); // untouched
        Assert.Equal(1, target["scale"]!["x"]!.GetValue<int>());
    }

    [Fact]
    public void Arrays_are_replaced_as_a_whole()
    {
        var target = Obj("""{ "list": [1, 2, 3] }""");

        JsonMerge.Apply(target, Obj("""{ "list": [9] }"""));

        Assert.Equal("[9]", target["list"]!.ToJsonString());
    }

    [Fact]
    public void Null_removes_a_property()
    {
        var target = Obj("""{ "a": 1, "b": { "c": 2 } }""");

        JsonMerge.Apply(target, Obj("""{ "a": null, "b": { "c": null } }"""));

        Assert.False(target.ContainsKey("a"));
        Assert.True(target["b"] is JsonObject b && !b.ContainsKey("c"));
    }

    [Fact]
    public void Null_for_a_missing_property_is_harmless()
    {
        var target = Obj("""{ "a": 1 }""");

        JsonMerge.Apply(target, Obj("""{ "missing": null }"""));

        Assert.Single(target);
    }

    [Fact]
    public void A_new_nested_object_is_copied_without_its_nulls()
    {
        var target = new JsonObject();

        JsonMerge.Apply(target, Obj("""{ "o": { "keep": 1, "drop": null } }"""));

        Assert.Equal("""{"o":{"keep":1}}""", target.ToJsonString());
    }

    [Fact]
    public void The_target_does_not_share_nodes_with_the_patch()
    {
        var patch = Obj("""{ "o": { "x": 1 }, "arr": [1] }""");
        var target = new JsonObject();

        JsonMerge.Apply(target, patch);
        ((JsonObject)target["o"]!)["x"] = 99;
        ((JsonArray)target["arr"]!).Add(2);

        Assert.Equal("""{"o":{"x":1},"arr":[1]}""", patch.ToJsonString());
    }

    [Fact]
    public void An_object_patch_replaces_a_scalar_target()
    {
        var target = Obj("""{ "a": 5 }""");

        JsonMerge.Apply(target, Obj("""{ "a": { "x": 1 } }"""));

        Assert.Equal("""{"a":{"x":1}}""", target.ToJsonString());
    }
}
