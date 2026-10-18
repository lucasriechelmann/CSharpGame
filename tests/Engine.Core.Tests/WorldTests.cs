using Engine.Core.Ecs;

namespace Engine.Core.Tests;

public class WorldTests
{
    private struct Position { public float X, Y; }
    private struct Velocity { public float X, Y; }
    private struct Tag { }

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

    [Fact]
    public void Command_buffer_applies_changes_after_the_query()
    {
        var world = new World();
        var doomed = world.CreateEntity();
        world.Add(doomed, new Position { X = -1 });
        var survivor = world.CreateEntity();
        world.Add(survivor, new Position { X = 1 });

        var buffer = new CommandBuffer();
        world.Query<Position>((Entity e, ref Position p) =>
        {
            if (p.X < 0) buffer.Destroy(e);
            else buffer.Add(e, new Velocity { X = 7 });
        });

        Assert.Equal(2, buffer.Count);
        buffer.Playback(world);

        Assert.Equal(0, buffer.Count);
        Assert.False(world.IsAlive(doomed));
        Assert.Equal(7, world.Get<Velocity>(survivor).X);
    }

    [Fact]
    public void Command_buffer_skips_entities_that_died_before_playback()
    {
        var world = new World();
        var e = world.CreateEntity();
        var buffer = new CommandBuffer();
        buffer.Add(e, new Position());
        buffer.Destroy(e);
        world.DestroyEntity(e);

        buffer.Playback(world); // must not throw
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void System_group_runs_in_insertion_order()
    {
        var world = new World();
        var log = new List<string>();
        var group = new SystemGroup()
            .Add(new LogSystem("first", log))
            .Add(new LogSystem("second", log));

        group.Run(world, 0.016);

        Assert.Equal(new[] { "first", "second" }, log);
    }

    [Fact]
    public void Movement_system_integrates_velocity()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());
        world.Add(e, new Velocity { X = 4, Y = -2 });

        var group = new SystemGroup().Add(new MovementSystem());
        group.Run(world, 0.5);

        Assert.Equal(2, world.Get<Position>(e).X);
        Assert.Equal(-1, world.Get<Position>(e).Y);
    }

    private sealed class LogSystem(string name, List<string> log) : ISystem
    {
        public void Update(World world, double dt) => log.Add(name);
    }

    private sealed class MovementSystem : ISystem
    {
        public void Update(World world, double dt) =>
            world.Query<Position, Velocity>((Entity _, ref Position p, ref Velocity v) =>
            {
                p.X += v.X * (float)dt;
                p.Y += v.Y * (float)dt;
            });
    }
}
