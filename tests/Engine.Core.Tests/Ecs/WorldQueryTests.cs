using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class WorldQueryTests
{
    [Fact]
    public void Query_visits_only_entities_with_all_components_and_writes_back()
    {
        var world = new World();
        var moving = world.CreateEntity();
        world.Add(moving, new Position { X = 0, Y = 0 });
        world.Add(moving, new Velocity { X = 1, Y = 2 });

        var still = world.CreateEntity();
        world.Add(still, new Position { X = 9, Y = 9 });

        var velocityOnly = world.CreateEntity();
        world.Add(velocityOnly, new Velocity { X = 5, Y = 5 });

        int visited = 0;
        world.Query<Position, Velocity>((Entity _, ref Position p, ref Velocity v) =>
        {
            p.X += v.X;
            p.Y += v.Y;
            visited++;
        });

        Assert.Equal(1, visited);
        Assert.Equal(1, world.Get<Position>(moving).X);
        Assert.Equal(2, world.Get<Position>(moving).Y);
        Assert.Equal(9, world.Get<Position>(still).X);
    }

    [Fact]
    public void Two_component_query_gives_same_result_regardless_of_which_pool_is_smaller()
    {
        var world = new World();
        for (int i = 0; i < 10; i++)
        {
            var e = world.CreateEntity();
            world.Add(e, new Position { X = i });
            if (i % 5 == 0) world.Add(e, new Velocity()); // Velocity pool is smaller

            var f = world.CreateEntity();
            world.Add(f, new Velocity());                  // extra Velocity-only entities
        }

        var seen = new List<float>();
        world.Query<Position, Velocity>((Entity _, ref Position p, ref Velocity _) => seen.Add(p.X));
        seen.Sort();
        Assert.Equal(new float[] { 0, 5 }, seen);

        var seen2 = new List<float>();
        world.Query<Velocity, Position>((Entity _, ref Velocity _, ref Position p) => seen2.Add(p.X));
        seen2.Sort();
        Assert.Equal(new float[] { 0, 5 }, seen2);
    }

    [Fact]
    public void Three_component_query()
    {
        var world = new World();
        var all = world.CreateEntity();
        world.Add(all, new Position());
        world.Add(all, new Velocity());
        world.Add(all, new Tag());

        var partial = world.CreateEntity();
        world.Add(partial, new Position());
        world.Add(partial, new Velocity());

        var hits = new List<Entity>();
        world.Query<Position, Velocity, Tag>((Entity e, ref Position _, ref Velocity _, ref Tag _) => hits.Add(e));
        Assert.Equal(new[] { all }, hits);
    }

    [Fact]
    public void Query_passes_entity_handle_that_is_alive()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());

        Entity seen = default;
        world.Query<Position>((Entity entity, ref Position _) => seen = entity);
        Assert.Equal(e, seen);
        Assert.True(world.IsAlive(seen));
    }

    [Fact]
    public void Query_over_unregistered_component_does_nothing()
    {
        var world = new World();
        world.CreateEntity();
        int visited = 0;
        world.Query<Position>((Entity _, ref Position _) => visited++);
        Assert.Equal(0, visited);
    }

    [Fact]
    public void Structural_changes_during_a_query_throw_and_the_lock_is_released()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());

        Assert.Throws<InvalidOperationException>(() =>
            world.Query<Position>((Entity entity, ref Position _) => world.DestroyEntity(entity)));
        Assert.Throws<InvalidOperationException>(() =>
            world.Query<Position>((Entity entity, ref Position _) => world.Add(entity, new Velocity())));
        Assert.Throws<InvalidOperationException>(() =>
            world.Query<Position>((Entity _, ref Position _) => world.CreateEntity()));

        // After the failed queries the world must be usable again.
        world.Add(e, new Velocity());
        Assert.True(world.Has<Velocity>(e));
    }
}
