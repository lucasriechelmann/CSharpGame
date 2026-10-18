using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class WorldLookupTests
{
    private readonly World _world = new();

    private Entity Named(string name)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Name.Of(name));
        return e;
    }

    [Fact]
    public void FindByName_returns_the_entity_with_that_exact_name()
    {
        Named("Sun");
        var player = Named("Player");

        Assert.Equal(player, _world.FindByName("Player"));
        Assert.True(_world.FindByName("player").IsNull); // ordinal: case matters
        Assert.True(_world.FindByName("Nobody").IsNull);
    }

    [Fact]
    public void FindByName_on_a_world_without_names_is_null_and_a_null_name_throws()
    {
        Assert.True(_world.FindByName("Player").IsNull);
        Assert.Throws<ArgumentNullException>(() => _world.FindByName(null!));
    }

    [Fact]
    public void RequireByName_throws_naming_the_missing_entity()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _world.RequireByName("Camera"));

        Assert.Contains("Camera", ex.Message);
    }

    [Fact]
    public void FindFirst_and_CountWith_look_at_one_component_kind()
    {
        Assert.True(_world.FindFirst<Position>().IsNull);
        Assert.Equal(0, _world.CountWith<Position>());

        var a = _world.CreateEntity();
        _world.Add(a, new Position());
        var b = _world.CreateEntity();
        _world.Add(b, new Position());

        Assert.Equal(a, _world.FindFirst<Position>());
        Assert.Equal(2, _world.CountWith<Position>());
        Assert.Equal(a, _world.RequireFirst<Position>());
        Assert.Throws<InvalidOperationException>(() => _world.RequireFirst<Velocity>());
    }

    [Fact]
    public void A_destroyed_entity_is_no_longer_found()
    {
        var player = Named("Player");

        _world.DestroyEntity(player);

        Assert.True(_world.FindByName("Player").IsNull);
    }
}
