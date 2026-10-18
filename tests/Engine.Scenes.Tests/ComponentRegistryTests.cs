using Engine.Core.Components;
using Engine.Scenes;
using Engine.Scenes.Tests.Fixtures;

namespace Engine.Scenes.Tests;

public class ComponentRegistryTests
{
    [Fact]
    public void Registered_components_are_known_by_name()
    {
        var registry = new ComponentRegistry().Register<Health>("Health").Register<Transform>("Transform");

        Assert.True(registry.Contains("Health"));
        Assert.False(registry.Contains("health")); // names are case-sensitive
        Assert.Equal(2, registry.Count);
        Assert.Equal(new[] { "Health", "Transform" }, registry.Names.ToArray());
    }

    [Fact]
    public void Duplicate_names_are_rejected()
    {
        var registry = new ComponentRegistry().Register<Health>("Health");

        Assert.Throws<ArgumentException>(() => registry.Register<Transform>("Health"));
    }

    [Fact]
    public void Registering_a_type_twice_is_rejected()
    {
        var registry = new ComponentRegistry().Register<Health>("Health");

        Assert.Throws<ArgumentException>(() => registry.Register<Health>("HP"));
    }

    [Fact]
    public void Describe_lists_name_type_and_default_value_in_registration_order()
    {
        var registry = new ComponentRegistry()
            .Register<Health>("Health")
            .Register("Transform", Transform.Identity);

        var infos = registry.Describe().ToArray();

        Assert.Equal(new[] { "Health", "Transform" }, infos.Select(i => i.Name).ToArray());
        Assert.Equal(typeof(Health), infos[0].Type);
        Assert.Equal(typeof(Transform), infos[1].Type);
        Assert.Equal(Transform.Identity, (Transform)infos[1].DefaultValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_names_are_rejected(string name)
    {
        Assert.Throws<ArgumentException>(() => new ComponentRegistry().Register<Health>(name));
    }
}
