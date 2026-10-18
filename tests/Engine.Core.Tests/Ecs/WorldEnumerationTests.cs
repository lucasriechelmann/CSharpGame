using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class WorldEnumerationTests
{
    [Fact]
    public void Entities_lists_live_entities_in_id_order()
    {
        var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        var c = world.CreateEntity();

        Assert.Equal(new[] { a, b, c }, world.Entities.ToArray());
    }

    [Fact]
    public void Destroyed_entities_are_not_listed()
    {
        var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        var c = world.CreateEntity();
        world.DestroyEntity(b);

        Assert.Equal(new[] { a, c }, world.Entities.ToArray());
    }

    [Fact]
    public void A_recycled_slot_is_listed_with_its_new_generation()
    {
        var world = new World();
        var old = world.CreateEntity();
        world.DestroyEntity(old);
        var fresh = world.CreateEntity();

        var listed = Assert.Single(world.Entities);
        Assert.Equal(fresh, listed);
        Assert.NotEqual(old, listed);
    }

    [Fact]
    public void An_empty_world_lists_nothing()
    {
        Assert.Empty(new World().Entities);
    }

    [Fact]
    public void Clear_destroys_every_entity_and_component()
    {
        var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        world.Add(a, new Position { X = 1 });
        world.Add(b, new Velocity { X = 2 });

        world.Clear();

        Assert.Equal(0, world.EntityCount);
        Assert.Empty(world.Entities);
        Assert.False(world.IsAlive(a));
        Assert.False(world.IsAlive(b));

        int visited = 0;
        world.Query<Position>((Entity _, ref Position _) => visited++);
        world.Query<Velocity>((Entity _, ref Velocity _) => visited++);
        Assert.Equal(0, visited);
    }

    [Fact]
    public void The_world_is_fully_usable_after_Clear()
    {
        var world = new World();
        var before = world.CreateEntity();
        world.Add(before, new Position { X = 9 });
        world.Clear();

        var after = world.CreateEntity();
        world.Add(after, new Position { X = 5 });

        Assert.False(world.IsAlive(before)); // stale handle stays stale even though the slot was reused
        Assert.True(world.IsAlive(after));
        Assert.Equal(5, world.Get<Position>(after).X);
        Assert.Equal(1, world.EntityCount);
    }

    [Fact]
    public void Clear_during_a_query_throws()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());

        Assert.Throws<InvalidOperationException>(() =>
            world.Query<Position>((Entity _, ref Position _) => world.Clear()));
    }
}
