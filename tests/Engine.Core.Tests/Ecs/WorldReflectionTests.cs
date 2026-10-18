using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class WorldReflectionTests
{
    [Fact]
    public void GetComponentTypes_lists_what_the_entity_has()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());
        world.Add(e, new Velocity());
        var other = world.CreateEntity();
        world.Add(other, new Tag());

        var types = new List<Type>();
        world.GetComponentTypes(e, types);

        Assert.Equal(2, types.Count);
        Assert.Contains(typeof(Position), types);
        Assert.Contains(typeof(Velocity), types);
    }

    [Fact]
    public void GetComponentTypes_throws_for_a_dead_entity()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.DestroyEntity(e);

        Assert.Throws<InvalidOperationException>(() => world.GetComponentTypes(e, new List<Type>()));
    }

    [Fact]
    public void GetBoxed_returns_a_copy_of_the_value()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position { X = 3, Y = 4 });

        var boxed = (Position)world.GetBoxed(e, typeof(Position));

        Assert.Equal(3, boxed.X);
        Assert.Equal(4, boxed.Y);
    }

    [Fact]
    public void GetBoxed_throws_when_the_component_is_missing()
    {
        var world = new World();
        var e = world.CreateEntity();

        Assert.Throws<InvalidOperationException>(() => world.GetBoxed(e, typeof(Position)));
    }

    [Fact]
    public void SetBoxed_writes_back_into_the_world()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position { X = 1, Y = 1 });

        world.SetBoxed(e, typeof(Position), new Position { X = 9, Y = 8 });

        Assert.Equal(9, world.Get<Position>(e).X);
        Assert.Equal(8, world.Get<Position>(e).Y);
    }

    [Fact]
    public void SetBoxed_rejects_a_value_of_the_wrong_type()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());

        Assert.Throws<ArgumentException>(() => world.SetBoxed(e, typeof(Position), new Velocity()));
    }

    [Fact]
    public void AddBoxed_creates_the_pool_on_first_use_and_stores_the_value()
    {
        var world = new World();
        var e = world.CreateEntity();

        world.AddBoxed(e, typeof(Position), new Position { X = 5 });

        Assert.True(world.Has<Position>(e));
        Assert.Equal(5, world.Get<Position>(e).X);
    }

    [Fact]
    public void AddBoxed_without_a_value_adds_the_default()
    {
        var world = new World();
        var e = world.CreateEntity();

        world.AddBoxed(e, typeof(Position));

        Assert.Equal(0, world.Get<Position>(e).X);
    }

    [Fact]
    public void AddBoxed_rejects_a_duplicate()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());

        Assert.Throws<InvalidOperationException>(() => world.AddBoxed(e, typeof(Position)));
    }

    [Fact]
    public void AddBoxed_is_rejected_during_a_query()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());
        var target = world.CreateEntity();

        Assert.Throws<InvalidOperationException>(() =>
            world.Query<Position>((Entity _, ref Position __) => world.AddBoxed(target, typeof(Velocity))));
    }

    [Fact]
    public void Has_by_type_reflects_add_and_remove()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.False(world.Has(e, typeof(Position)));

        world.AddBoxed(e, typeof(Position));
        Assert.True(world.Has(e, typeof(Position)));

        Assert.True(world.Remove(e, typeof(Position)));
        Assert.False(world.Has(e, typeof(Position)));
        Assert.False(world.Remove(e, typeof(Position)));
    }
}
