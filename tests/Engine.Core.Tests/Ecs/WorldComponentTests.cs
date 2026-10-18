using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class WorldComponentTests
{
    [Fact]
    public void Add_and_get_component_by_reference()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position { X = 1, Y = 2 });

        world.Get<Position>(e).X = 10;

        Assert.Equal(10, world.Get<Position>(e).X);
        Assert.Equal(2, world.Get<Position>(e).Y);
    }

    [Fact]
    public void Adding_a_duplicate_component_throws_but_Set_overwrites()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position { X = 1 });

        Assert.Throws<InvalidOperationException>(() => world.Add(e, new Position { X = 2 }));

        world.Set(e, new Position { X = 3 });
        Assert.Equal(3, world.Get<Position>(e).X);
    }

    [Fact]
    public void Has_TryGet_and_Remove()
    {
        var world = new World();
        var e = world.CreateEntity();

        Assert.False(world.Has<Position>(e));
        Assert.False(world.TryGet<Position>(e, out _));
        Assert.False(world.Remove<Position>(e));

        world.Add(e, new Position { X = 5 });
        Assert.True(world.Has<Position>(e));
        Assert.True(world.TryGet<Position>(e, out var p));
        Assert.Equal(5, p.X);

        Assert.True(world.Remove<Position>(e));
        Assert.False(world.Has<Position>(e));
    }

    [Fact]
    public void Get_missing_component_throws()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.Throws<InvalidOperationException>(() => world.Get<Position>(e));
    }

    [Fact]
    public void Swap_remove_keeps_other_components_intact()
    {
        var world = new World();
        var entities = new Entity[5];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.CreateEntity();
            world.Add(entities[i], new Position { X = i });
        }

        world.Remove<Position>(entities[1]); // last element is swapped into index 1
        world.Remove<Position>(entities[4]);

        Assert.Equal(0, world.Get<Position>(entities[0]).X);
        Assert.Equal(2, world.Get<Position>(entities[2]).X);
        Assert.Equal(3, world.Get<Position>(entities[3]).X);
        Assert.False(world.Has<Position>(entities[1]));
        Assert.False(world.Has<Position>(entities[4]));
    }

    [Fact]
    public void Storage_grows_past_initial_capacity()
    {
        var world = new World();
        var entities = new List<Entity>();
        for (int i = 0; i < 1000; i++)
        {
            var e = world.CreateEntity();
            world.Add(e, new Position { X = i });
            entities.Add(e);
        }

        for (int i = 0; i < 1000; i++)
            Assert.Equal(i, world.Get<Position>(entities[i]).X);
    }
}
