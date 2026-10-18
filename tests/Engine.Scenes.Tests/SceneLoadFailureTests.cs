using Engine.Scenes.Tests.Fixtures;

namespace Engine.Scenes.Tests;

public sealed class SceneLoadFailureTests : IDisposable
{
    private readonly SceneTestBed _bed = new();

    public void Dispose() => _bed.Dispose();

    private void AssertFailsWithoutChangingTheWorld<TException>(string json) where TException : Exception
    {
        int before = _bed.World.EntityCount;

        Assert.Throws<TException>(() => _bed.Scenes.Load(json));

        Assert.Equal(before, _bed.World.EntityCount);
    }

    [Fact]
    public void A_bad_second_entity_leaves_no_trace_of_the_good_first_one()
    {
        AssertFailsWithoutChangingTheWorld<InvalidOperationException>("""
            { "version": 1, "entities": [
                { "components": { "Health": { "value": 1 } } },
                { "components": { "Healthh": { "value": 2 } } }
            ] }
            """);
    }

    [Fact]
    public void Unknown_component_names_fail_and_name_the_component()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _bed.Scenes.Load(
            """{ "version": 1, "entities": [ { "components": { "Trnsform": {} } } ] }"""));

        Assert.Contains("Trnsform", ex.Message);
    }

    [Fact]
    public void Invalid_component_data_fails_and_names_the_component()
    {
        string json = """{ "version": 1, "entities": [ { "components": { "Health": { "value": "lots" } } } ] }""";

        var ex = Assert.Throws<InvalidOperationException>(() => _bed.Scenes.Load(json));

        Assert.Contains("Health", ex.Message);
        Assert.Equal(0, _bed.World.EntityCount);
    }

    [Fact]
    public void Unknown_asset_names_in_a_component_fail_without_creating_entities()
    {
        // A missing file surfaces as FileNotFoundException (naming the asset), the same as a missing prefab.
        AssertFailsWithoutChangingTheWorld<FileNotFoundException>(
            """{ "version": 1, "entities": [ { "components": { "TextRef": { "asset": "missing.txt" } } } ] }""");
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[]")]
    [InlineData("""{ "entities": [] }""")]                          // no version
    [InlineData("""{ "version": 99, "entities": [] }""")]            // from the future
    [InlineData("""{ "version": 0, "entities": [] }""")]
    [InlineData("""{ "version": 1 }""")]                             // no entities array
    [InlineData("""{ "version": 1, "entities": [ 5 ] }""")]          // entity is not an object
    [InlineData("""{ "version": 1, "entities": [ { "component": {} } ] }""")]   // typo in "components"
    [InlineData("""{ "version": 1, "entities": [ { "components": [] } ] }""")]  // components must be an object
    public void Malformed_scenes_are_rejected_and_the_world_is_unchanged(string json)
    {
        AssertFailsWithoutChangingTheWorld<InvalidOperationException>(json);
    }
}
