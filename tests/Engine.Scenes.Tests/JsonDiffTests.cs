using System.Text.Json.Nodes;

namespace Engine.Scenes.Tests;

public class JsonDiffTests
{
    private static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;

    [Fact]
    public void Equal_objects_have_an_empty_diff()
    {
        var a = Obj("""{ "x": 1, "child": { "y": [1, 2] } }""");

        Assert.Empty(JsonMerge.Diff(a, (JsonObject)a.DeepClone()));
    }

    [Fact]
    public void A_changed_scalar_is_in_the_diff()
    {
        var diff = JsonMerge.Diff(Obj("""{ "x": 1, "y": 2 }"""), Obj("""{ "x": 1, "y": 5 }"""));

        Assert.Equal("""{"y":5}""", diff.ToJsonString());
    }

    [Fact]
    public void A_changed_nested_value_is_diffed_only_as_deep_as_it_differs()
    {
        var diff = JsonMerge.Diff(Obj("""{ "p": { "x": 1, "y": 2 } }"""), Obj("""{ "p": { "x": 1, "y": 9 } }"""));

        Assert.Equal("""{"p":{"y":9}}""", diff.ToJsonString());
    }

    [Fact]
    public void A_changed_array_is_replaced_whole()
    {
        var diff = JsonMerge.Diff(Obj("""{ "a": [1, 2, 3] }"""), Obj("""{ "a": [1, 2, 4] }"""));

        Assert.Equal("""{"a":[1,2,4]}""", diff.ToJsonString());
    }

    [Fact]
    public void A_property_only_in_the_target_is_added()
    {
        var diff = JsonMerge.Diff(Obj("""{ "x": 1 }"""), Obj("""{ "x": 1, "extra": { "z": 3 } }"""));

        Assert.Equal("""{"extra":{"z":3}}""", diff.ToJsonString());
    }

    [Fact]
    public void A_property_only_in_the_base_becomes_null()
    {
        var diff = JsonMerge.Diff(Obj("""{ "x": 1, "gone": 2 }"""), Obj("""{ "x": 1 }"""));

        Assert.Equal("""{"gone":null}""", diff.ToJsonString());
    }

    [Fact]
    public void Numbers_that_are_equal_in_value_are_not_a_difference()
    {
        var diff = JsonMerge.Diff(Obj("""{ "x": 48.0 }"""), Obj("""{ "x": 48 }"""));

        Assert.Empty(diff);
    }

    [Fact]
    public void The_diff_is_not_aliased_to_the_target()
    {
        var target = Obj("""{ "p": { "x": 1 } }""");

        var diff = JsonMerge.Diff(new JsonObject(), target);
        ((JsonObject)diff["p"]!)["x"] = 99;

        Assert.Equal(1, (int)((JsonObject)target["p"]!)["x"]!);
    }

    [Theory]
    [InlineData("""{ "a": 1, "b": { "c": 2, "d": [1, 2] }, "e": 5 }""", """{ "a": 1, "b": { "c": 3, "d": [1] } }""")]
    [InlineData("""{ }""", """{ "a": { "b": { "c": 1 } } }""")]
    [InlineData("""{ "a": { "b": 1 }, "z": 0 }""", """{ "a": { "b": 1 }, "n": null_replacement }""")]
    public void Applying_the_diff_to_the_base_gives_the_target(string fromJson, string toJson)
    {
        var from = Obj(fromJson.Replace("null_replacement", "7"));
        var to = Obj(toJson.Replace("null_replacement", "7"));

        var rebuilt = (JsonObject)from.DeepClone();
        JsonMerge.Apply(rebuilt, JsonMerge.Diff(from, to));

        Assert.True(JsonNode.DeepEquals(to, rebuilt), $"expected {to.ToJsonString()} but got {rebuilt.ToJsonString()}");
    }
}
