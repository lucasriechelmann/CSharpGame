using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class WorldEntityTests
{
    [Fact]
    public void Created_entities_are_alive_and_distinct()
    {
        var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();

        Assert.True(world.IsAlive(a));
        Assert.True(world.IsAlive(b));
        Assert.NotEqual(a, b);
        Assert.Equal(2, world.EntityCount);
    }

    [Fact]
    public void Null_entity_is_never_alive()
    {
        var world = new World();
        Assert.False(world.IsAlive(Entity.Null));
        Assert.False(world.IsAlive(default));
    }

    [Fact]
    public void Destroyed_handle_goes_stale_even_after_slot_is_reused()
    {
        var world = new World();
        var old = world.CreateEntity();
        world.DestroyEntity(old);
        var fresh = world.CreateEntity();

        Assert.Equal(old.Id, fresh.Id);          // slot recycled
        Assert.NotEqual(old.Generation, fresh.Generation);
        Assert.False(world.IsAlive(old));
        Assert.True(world.IsAlive(fresh));
        Assert.Equal(1, world.EntityCount);
    }

    [Fact]
    public void Operations_on_dead_entities_throw()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.DestroyEntity(e);

        Assert.Throws<InvalidOperationException>(() => world.Add(e, new Position()));
        Assert.Throws<InvalidOperationException>(() => world.DestroyEntity(e));
        Assert.False(world.Has<Position>(e));
    }

    [Fact]
    public void Destroying_an_entity_removes_all_its_components()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());
        world.Add(e, new Velocity());
        world.DestroyEntity(e);

        int visited = 0;
        world.Query<Position>((Entity _, ref Position _) => visited++);
        world.Query<Velocity>((Entity _, ref Velocity _) => visited++);
        Assert.Equal(0, visited);
    }
}
